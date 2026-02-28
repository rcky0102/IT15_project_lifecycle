/* ===================================================================
   messenger.js  –  Facebook-Messenger-style real-time chat client
   =================================================================== */

(function () {
    'use strict';

    // ── DOM refs ──────────────────────────────────────────────────────
    const sidebar       = document.getElementById('messengerSidebar');
    const convList      = document.getElementById('convList');
    const peopleList    = document.getElementById('peopleList');
    const chatArea      = document.getElementById('chatArea');
    const chatPlaceholder = document.getElementById('chatPlaceholder');
    const chatActive    = document.getElementById('chatActive');
    const chatHeaderAvatar   = document.getElementById('chatHeaderAvatar');
    const chatHeaderName     = document.getElementById('chatHeaderName');
    const chatHeaderStatus   = document.getElementById('chatHeaderStatus');
    const messagesContainer  = document.getElementById('messagesContainer');
    const msgInput      = document.getElementById('msgInput');
    const sendBtn       = document.getElementById('sendBtn');
    const searchInput   = document.getElementById('messengerSearch');
    const tabChats      = document.getElementById('tabChats');
    const tabPeople     = document.getElementById('tabPeople');
    const backBtn       = document.getElementById('chatBackBtn');

    let currentConversationId = null;
    let currentUserId = null;
    let connection = null;

    // ── Initialize ────────────────────────────────────────────────────
    async function init() {
        // Get current user id from a meta tag we'll add to layout
        const meta = document.querySelector('meta[name="current-user-id"]');
        currentUserId = meta ? meta.content : null;

        setupTabs();
        setupSearch();
        setupInput();
        setupBackButton();

        await loadConversations();
        await loadContacts();
        await initSignalR();
        await updateNavBadge();
    }

    // ── SignalR ───────────────────────────────────────────────────────
    async function initSignalR() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/chatHub')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveMessage', (msg) => {
            if (msg.conversationId === currentConversationId) {
                appendMessage(msg, msg.senderId === currentUserId);
                scrollToBottom();
                // Mark as read
                fetch(`/api/messages/conversations/${currentConversationId}/read`, { method: 'POST' });
            }
            // Refresh conversation list
            loadConversations();
            updateNavBadge();
        });

        connection.on('ConversationUpdated', (data) => {
            loadConversations();
            updateNavBadge();
        });

        try {
            await connection.start();
        } catch (err) {
            console.error('SignalR connection failed:', err);
            // Retry after 5s
            setTimeout(initSignalR, 5000);
        }
    }

    // ── Tabs ──────────────────────────────────────────────────────────
    function setupTabs() {
        tabChats?.addEventListener('click', () => {
            tabChats.classList.add('active');
            tabPeople.classList.remove('active');
            convList.style.display = 'block';
            peopleList.style.display = 'none';
            searchInput.placeholder = 'Search conversations...';
        });

        tabPeople?.addEventListener('click', () => {
            tabPeople.classList.add('active');
            tabChats.classList.remove('active');
            convList.style.display = 'none';
            peopleList.style.display = 'block';
            searchInput.placeholder = 'Search people...';
        });
    }

    // ── Search ────────────────────────────────────────────────────────
    function setupSearch() {
        let debounce;
        searchInput?.addEventListener('input', () => {
            clearTimeout(debounce);
            debounce = setTimeout(() => {
                const q = searchInput.value.trim().toLowerCase();
                const isPeopleTab = tabPeople?.classList.contains('active');

                if (isPeopleTab) {
                    filterPeople(q);
                } else {
                    filterConversations(q);
                }
            }, 250);
        });
    }

    function filterConversations(q) {
        const items = convList.querySelectorAll('.messenger-conv-item');
        items.forEach(item => {
            const name = item.dataset.name?.toLowerCase() || '';
            item.style.display = name.includes(q) ? '' : 'none';
        });
    }

    function filterPeople(q) {
        const items = peopleList.querySelectorAll('.messenger-people-item');
        items.forEach(item => {
            const name = item.dataset.name?.toLowerCase() || '';
            const role = item.dataset.role?.toLowerCase() || '';
            item.style.display = (name.includes(q) || role.includes(q)) ? '' : 'none';
        });
    }

    // ── Back button (mobile) ──────────────────────────────────────────
    function setupBackButton() {
        backBtn?.addEventListener('click', () => {
            sidebar.classList.remove('hidden-mobile');
            chatPlaceholder.style.display = '';
            chatActive.style.display = 'none';

            // Leave conversation SignalR group
            if (currentConversationId && connection) {
                connection.invoke('LeaveConversation', currentConversationId.toString());
            }
            currentConversationId = null;
        });
    }

    // ── Load Conversations ────────────────────────────────────────────
    async function loadConversations() {
        try {
            const res = await fetch('/api/messages/conversations');
            if (!res.ok) {
                console.error('Failed to load conversations. Status:', res.status);
                return;
            }
            const data = await res.json();
            renderConversations(data);
        } catch (err) {
            console.error('Failed to load conversations:', err);
        }
    }

    function renderConversations(conversations) {
        if (!convList) return;
        convList.innerHTML = '';

        if (conversations.length === 0) {
            convList.innerHTML = `
                <div class="messenger-empty" style="padding: 40px 20px;">
                    <i class="fas fa-comments"></i>
                    <p>No conversations yet</p>
                    <span style="font-size: .8rem; color: #65676b;">Click "People" to start chatting</span>
                </div>`;
            return;
        }

        conversations.forEach(conv => {
            const item = document.createElement('div');
            item.className = 'messenger-conv-item' + (conv.id === currentConversationId ? ' active' : '');
            item.dataset.id = conv.id;
            item.dataset.name = conv.displayName;

            const previewText = conv.lastMessage
                ? `<span>${conv.lastMessage.senderName}: ${escapeHtml(conv.lastMessage.content)}</span>
                   <span class="time-sep">·</span>
                   <span>${conv.lastMessage.timeAgo}</span>`
                : '<span>No messages yet</span>';

            const unreadBadge = conv.unreadCount > 0
                ? `<div class="messenger-conv-unread">${conv.unreadCount}</div>`
                : '';

            item.innerHTML = `
                <div class="messenger-conv-avatar">${escapeHtml(conv.initials)}</div>
                <div class="messenger-conv-info">
                    <div class="messenger-conv-name">${escapeHtml(conv.displayName)}</div>
                    <div class="messenger-conv-preview">${previewText}</div>
                </div>
                ${unreadBadge}`;

            item.addEventListener('click', () => openConversation(conv.id, conv.displayName, conv.initials));
            convList.appendChild(item);
        });
    }

    // ── Load Contacts ─────────────────────────────────────────────────
    async function loadContacts() {
        try {
            const res = await fetch('/api/messages/contacts');
            if (!res.ok) {
                console.error('Failed to load contacts. Status:', res.status);
                peopleList.innerHTML = `
                    <div class="messenger-empty" style="padding: 40px 20px;">
                        <i class="fas fa-exclamation-circle"></i>
                        <p>Failed to load contacts</p>
                    </div>`;
                return;
            }
            const data = await res.json();
            renderContacts(data);
        } catch (err) {
            console.error('Failed to load contacts:', err);
        }
    }

    function renderContacts(contacts) {
        if (!peopleList) return;
        peopleList.innerHTML = '';

        if (contacts.length === 0) {
            peopleList.innerHTML = `
                <div class="messenger-empty" style="padding: 40px 20px;">
                    <i class="fas fa-users"></i>
                    <p>No contacts found</p>
                </div>`;
            return;
        }

        contacts.forEach(c => {
            const item = document.createElement('div');
            item.className = 'messenger-people-item';
            item.dataset.name = c.name;
            item.dataset.role = c.role;
            item.dataset.userId = c.userId;

            item.innerHTML = `
                <div class="messenger-people-avatar">${escapeHtml(c.initials)}</div>
                <div class="messenger-people-info">
                    <div class="messenger-people-name">${escapeHtml(c.name)}</div>
                    <div class="messenger-people-role">${escapeHtml(c.role)} ${c.department ? '· ' + escapeHtml(c.department) : ''}</div>
                </div>`;

            item.addEventListener('click', () => startDirectChat(c.userId, c.name, c.initials));
            peopleList.appendChild(item);
        });
    }

    // ── Start Direct Chat ─────────────────────────────────────────────
    async function startDirectChat(otherUserId, name, initials) {
        try {
            const res = await fetch('/api/messages/conversations/direct', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ otherUserId })
            });

            if (!res.ok) {
                console.error('Failed to create conversation. Status:', res.status);
                alert('Failed to start conversation. Please try again.');
                return;
            }

            const data = await res.json();
            if (data.conversationId) {
                // Switch to chats tab
                tabChats?.click();
                await loadConversations();
                openConversation(data.conversationId, name, initials);
            }
        } catch (err) {
            console.error('Failed to create conversation:', err);
            alert('Failed to start conversation. Please try again.');
        }
    }

    // ── Open Conversation ─────────────────────────────────────────────
    async function openConversation(convId, name, initials) {
        // Leave previous conversation group
        if (currentConversationId && connection) {
            connection.invoke('LeaveConversation', currentConversationId.toString()).catch(() => {});
        }

        currentConversationId = convId;

        // Update header
        chatHeaderAvatar.textContent = initials;
        chatHeaderName.textContent = name;
        chatHeaderStatus.textContent = 'Active';

        // Show chat, hide placeholder
        chatPlaceholder.style.display = 'none';
        chatActive.style.display = 'flex';

        // On mobile, hide sidebar
        sidebar?.classList.add('hidden-mobile');

        // Highlight active conversation
        convList.querySelectorAll('.messenger-conv-item').forEach(item => {
            item.classList.toggle('active', parseInt(item.dataset.id) === convId);
        });

        // Load messages
        await loadMessages(convId);

        // Join SignalR group
        if (connection) {
            connection.invoke('JoinConversation', convId.toString()).catch(() => {});
        }

        // Mark as read
        fetch(`/api/messages/conversations/${convId}/read`, { method: 'POST' });

        // Focus input
        msgInput?.focus();

        // Refresh conversation list to update unread
        setTimeout(loadConversations, 300);
        updateNavBadge();
    }

    // ── Load Messages ─────────────────────────────────────────────────
    async function loadMessages(convId) {
        messagesContainer.innerHTML = `
            <div class="messenger-loading">
                <div class="spinner"></div>
            </div>`;

        try {
            const res = await fetch(`/api/messages/conversations/${convId}/messages`);
            const messages = await res.json();
            renderMessages(messages);
            scrollToBottom();
        } catch (err) {
            console.error('Failed to load messages:', err);
            messagesContainer.innerHTML = `
                <div class="messenger-empty">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Failed to load messages</p>
                </div>`;
        }
    }

    function renderMessages(messages) {
        messagesContainer.innerHTML = '';

        if (messages.length === 0) {
            messagesContainer.innerHTML = `
                <div class="messenger-empty">
                    <i class="fas fa-paper-plane"></i>
                    <p>No messages yet. Say hello!</p>
                </div>`;
            return;
        }

        let lastDate = '';
        messages.forEach(msg => {
            const msgDate = new Date(msg.sentAt).toLocaleDateString('en-US', {
                weekday: 'long', month: 'short', day: 'numeric'
            });
            if (msgDate !== lastDate) {
                lastDate = msgDate;
                const sep = document.createElement('div');
                sep.className = 'msg-date-separator';
                sep.textContent = msgDate;
                messagesContainer.appendChild(sep);
            }
            appendMessage(msg, msg.isOwn);
        });
    }

    function appendMessage(msg, isOwn) {
        const row = document.createElement('div');
        row.className = 'msg-row ' + (isOwn ? 'own' : 'other');

        const time = new Date(msg.sentAt).toLocaleTimeString('en-US', {
            hour: 'numeric', minute: '2-digit'
        });

        row.innerHTML = `
            <div class="msg-avatar">${escapeHtml(msg.senderInitials || '??')}</div>
            <div>
                <div class="msg-bubble">${escapeHtml(msg.content)}</div>
                <div class="msg-time">${time}</div>
            </div>`;

        messagesContainer.appendChild(row);
    }

    function scrollToBottom() {
        if (messagesContainer) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
    }

    // ── Send message ──────────────────────────────────────────────────
    function setupInput() {
        msgInput?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        sendBtn?.addEventListener('click', () => sendMessage());
    }

    async function sendMessage() {
        const content = msgInput?.value?.trim();
        if (!content || !currentConversationId) return;

        msgInput.value = '';
        sendBtn.disabled = true;

        try {
            const res = await fetch(`/api/messages/conversations/${currentConversationId}/messages`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ content })
            });
            if (!res.ok) {
                console.error('Failed to send message. Status:', res.status);
                msgInput.value = content; // Restore on failure
            }
        } catch (err) {
            console.error('Failed to send message:', err);
            msgInput.value = content; // Restore on failure
        } finally {
            sendBtn.disabled = false;
            msgInput?.focus();
        }
    }

    // ── Nav badge (total unread) ──────────────────────────────────────
    async function updateNavBadge() {
        try {
            const res = await fetch('/api/messages/unread-count');
            const data = await res.json();
            const badges = document.querySelectorAll('.msg-nav-badge');
            badges.forEach(badge => {
                if (data.count > 0) {
                    badge.textContent = data.count > 99 ? '99+' : data.count;
                    badge.style.display = '';
                } else {
                    badge.style.display = 'none';
                }
            });
        } catch (err) {
            // Silently fail
        }
    }

    // Periodically update badge
    setInterval(updateNavBadge, 30000);

    // ── Helpers ───────────────────────────────────────────────────────
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ── Boot ──────────────────────────────────────────────────────────
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
