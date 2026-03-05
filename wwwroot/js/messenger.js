/* ===================================================================
   messenger.js  –  Facebook-Messenger-style real-time chat client
   =================================================================== */

(function () {
    'use strict';

    // ── Confirmation modal helper (emp-modal pattern) ─────────────
    function showConfirmModal(message) {
        return new Promise(function (resolve) {
            var existing = document.getElementById('messengerConfirmModal');
            if (existing) existing.remove();

            var backdrop = document.createElement('div');
            backdrop.id = 'messengerConfirmModal';
            backdrop.className = 'emp-modal-backdrop';
            backdrop.style.display = 'flex';
            backdrop.innerHTML =
                '<div class="emp-modal">' +
                    '<div class="emp-modal-icon"><i class="fas fa-question-circle"></i></div>' +
                    '<h5 class="emp-modal-title">Confirm Action</h5>' +
                    '<p class="emp-modal-message">' + message + '</p>' +
                    '<div class="emp-modal-actions">' +
                        '<button type="button" class="emp-btn-cancel" id="msgConfirmCancel">Cancel</button>' +
                        '<button type="button" class="emp-btn-confirm" id="msgConfirmOk">Confirm</button>' +
                    '</div>' +
                '</div>';
            document.body.appendChild(backdrop);

            function cleanup(result) {
                backdrop.remove();
                resolve(result);
            }

            document.getElementById('msgConfirmCancel').addEventListener('click', function () { cleanup(false); });
            document.getElementById('msgConfirmOk').addEventListener('click', function () { cleanup(true); });
            backdrop.addEventListener('click', function (e) { if (e.target === backdrop) cleanup(false); });
        });
    }

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
    const tabNewGroup   = document.getElementById('tabNewGroup');
    const backBtn       = document.getElementById('chatBackBtn');

    // Group creation DOM refs
    const groupPanel         = document.getElementById('groupPanel');
    const groupNameInput     = document.getElementById('groupNameInput');
    const groupMemberSearch  = document.getElementById('groupMemberSearch');
    const groupSelectedMembers = document.getElementById('groupSelectedMembers');
    const groupMembersList   = document.getElementById('groupMembersList');
    const groupMemberCount   = document.getElementById('groupMemberCount');
    const groupCreateBtn     = document.getElementById('groupCreateBtn');

    let currentConversationId = null;
    let currentUserId = null;
    let currentConvIsGroup = false;
    let currentConvCreatedBy = null;
    let connection = null;
    let allContacts = [];           // cached contacts for group selection
    let selectedGroupMembers = {};  // { userId: { name, initials } }

    // Group settings DOM refs
    const groupSettingsBtn      = document.getElementById('groupSettingsBtn');
    const groupSettingsDrawer   = document.getElementById('groupSettingsDrawer');
    const groupSettingsBackBtn  = document.getElementById('groupSettingsBackBtn');
    const groupRenameInput      = document.getElementById('groupRenameInput');
    const groupRenameSaveBtn    = document.getElementById('groupRenameSaveBtn');
    const groupSettingsMembers  = document.getElementById('groupSettingsMembers');
    const groupSettingsMemberCount = document.getElementById('groupSettingsMemberCount');
    const groupLeaveBtn         = document.getElementById('groupLeaveBtn');

    // ── Initialize ────────────────────────────────────────────────────
    async function init() {
        // Get current user id from a meta tag we'll add to layout
        const meta = document.querySelector('meta[name="current-user-id"]');
        currentUserId = meta ? meta.content : null;

        setupTabs();
        setupSearch();
        setupInput();
        setupBackButton();
        setupGroupCreation();
        setupGroupSettings();

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
            // If the current conversation's settings drawer is open, refresh it
            if (data.conversationId === currentConversationId && groupSettingsDrawer && groupSettingsDrawer.style.display !== 'none') {
                loadGroupDetails(currentConversationId);
            }
        });

        connection.on('RemovedFromGroup', (data) => {
            // If currently viewing this conversation, close it
            if (data.conversationId === currentConversationId) {
                chatPlaceholder.style.display = '';
                chatActive.style.display = 'none';
                if (groupSettingsDrawer) groupSettingsDrawer.style.display = 'none';
                currentConversationId = null;
                currentConvIsGroup = false;
                currentConvCreatedBy = null;
            }
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
            tabNewGroup?.classList.remove('active');
            convList.style.display = 'block';
            peopleList.style.display = 'none';
            if (groupPanel) groupPanel.style.display = 'none';
            searchInput.placeholder = 'Search conversations...';
            searchInput.style.display = '';
        });

        tabPeople?.addEventListener('click', () => {
            tabPeople.classList.add('active');
            tabChats.classList.remove('active');
            tabNewGroup?.classList.remove('active');
            convList.style.display = 'none';
            peopleList.style.display = 'block';
            if (groupPanel) groupPanel.style.display = 'none';
            searchInput.placeholder = 'Search people...';
            searchInput.style.display = '';
        });

        tabNewGroup?.addEventListener('click', () => {
            tabNewGroup.classList.add('active');
            tabChats.classList.remove('active');
            tabPeople.classList.remove('active');
            convList.style.display = 'none';
            peopleList.style.display = 'none';
            if (groupPanel) groupPanel.style.display = 'flex';
            searchInput.style.display = 'none';
            resetGroupPanel();
            renderGroupMembersList(allContacts);
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

            const avatarClass = conv.isGroup
                ? 'messenger-conv-avatar group-avatar'
                : 'messenger-conv-avatar';
            const avatarIcon = conv.isGroup
                ? `<i class="fas fa-users" style="font-size:.9rem;"></i>`
                : escapeHtml(conv.initials);

            item.innerHTML = `
                <div class="${avatarClass}">${avatarIcon}</div>
                <div class="messenger-conv-info">
                    <div class="messenger-conv-name">${escapeHtml(conv.displayName)}</div>
                    <div class="messenger-conv-preview">${previewText}</div>
                </div>
                ${unreadBadge}`;

            item.addEventListener('click', () => openConversation(conv.id, conv.displayName, conv.initials, conv.isGroup, conv.createdByUserId));
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
            allContacts = data;  // cache for group creation
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
                openConversation(data.conversationId, name, initials, false, null);
            }
        } catch (err) {
            console.error('Failed to create conversation:', err);
            alert('Failed to start conversation. Please try again.');
        }
    }

    // ── Open Conversation ─────────────────────────────────────────────
    async function openConversation(convId, name, initials, isGroup, createdByUserId) {
        // Leave previous conversation group
        if (currentConversationId && connection) {
            connection.invoke('LeaveConversation', currentConversationId.toString()).catch(() => {});
        }

        currentConversationId = convId;
        currentConvIsGroup = !!isGroup;
        currentConvCreatedBy = createdByUserId || null;

        // Update header
        chatHeaderAvatar.textContent = initials;
        chatHeaderName.textContent = name;
        chatHeaderStatus.textContent = 'Active';

        // Show/hide group settings button
        if (groupSettingsBtn) {
            groupSettingsBtn.style.display = currentConvIsGroup ? '' : 'none';
        }
        // Hide group settings drawer when switching conversations
        if (groupSettingsDrawer) groupSettingsDrawer.style.display = 'none';

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

    // ── Group Chat Creation ──────────────────────────────────────────
    function setupGroupCreation() {
        // Search within group member list
        groupMemberSearch?.addEventListener('input', () => {
            const q = groupMemberSearch.value.trim().toLowerCase();
            const filtered = allContacts.filter(c =>
                c.name.toLowerCase().includes(q) ||
                c.role.toLowerCase().includes(q) ||
                (c.department && c.department.toLowerCase().includes(q))
            );
            renderGroupMembersList(filtered);
        });

        // Create group button
        groupCreateBtn?.addEventListener('click', createGroupChat);
    }

    function resetGroupPanel() {
        selectedGroupMembers = {};
        if (groupNameInput) groupNameInput.value = '';
        if (groupMemberSearch) groupMemberSearch.value = '';
        updateGroupMemberCount();
        renderGroupSelectedChips();
    }

    function renderGroupMembersList(contacts) {
        if (!groupMembersList) return;
        groupMembersList.innerHTML = '';

        if (contacts.length === 0) {
            groupMembersList.innerHTML = `
                <div class="messenger-empty" style="padding: 20px;">
                    <p style="font-size:.85rem;color:#65676b;">No people found</p>
                </div>`;
            return;
        }

        contacts.forEach(c => {
            const item = document.createElement('div');
            item.className = 'group-member-item';
            const isSelected = !!selectedGroupMembers[c.userId];
            if (isSelected) item.classList.add('selected');

            item.innerHTML = `
                <div class="messenger-people-avatar" style="width:36px;height:36px;font-size:.7rem;">${escapeHtml(c.initials)}</div>
                <div class="messenger-people-info" style="flex:1;min-width:0;">
                    <div class="messenger-people-name" style="font-size:.85rem;">${escapeHtml(c.name)}</div>
                    <div class="messenger-people-role">${escapeHtml(c.role)}${c.department ? ' · ' + escapeHtml(c.department) : ''}</div>
                </div>
                <div class="group-member-check">
                    <i class="fas ${isSelected ? 'fa-check-circle' : 'fa-circle'}" style="color:${isSelected ? '#0866ff' : '#bcc0c4'};font-size:1.1rem;"></i>
                </div>`;

            item.addEventListener('click', () => toggleGroupMember(c));
            groupMembersList.appendChild(item);
        });
    }

    function toggleGroupMember(contact) {
        if (selectedGroupMembers[contact.userId]) {
            delete selectedGroupMembers[contact.userId];
        } else {
            selectedGroupMembers[contact.userId] = {
                name: contact.name,
                initials: contact.initials
            };
        }
        updateGroupMemberCount();
        renderGroupSelectedChips();
        // Re-render list to update check icons
        const q = groupMemberSearch?.value?.trim().toLowerCase() || '';
        const filtered = q
            ? allContacts.filter(c => c.name.toLowerCase().includes(q) || c.role.toLowerCase().includes(q))
            : allContacts;
        renderGroupMembersList(filtered);
    }

    function renderGroupSelectedChips() {
        if (!groupSelectedMembers) return;
        groupSelectedMembers.innerHTML = '';

        const ids = Object.keys(selectedGroupMembers);
        ids.forEach(uid => {
            const m = selectedGroupMembers[uid];
            const chip = document.createElement('div');
            chip.className = 'group-member-chip';
            chip.innerHTML = `
                <span>${escapeHtml(m.name)}</span>
                <button class="chip-remove" title="Remove"><i class="fas fa-times"></i></button>`;
            chip.querySelector('.chip-remove').addEventListener('click', (e) => {
                e.stopPropagation();
                delete selectedGroupMembers[uid];
                updateGroupMemberCount();
                renderGroupSelectedChips();
                const q = groupMemberSearch?.value?.trim().toLowerCase() || '';
                const filtered = q
                    ? allContacts.filter(c => c.name.toLowerCase().includes(q) || c.role.toLowerCase().includes(q))
                    : allContacts;
                renderGroupMembersList(filtered);
            });
            groupSelectedMembers.appendChild(chip);
        });
    }

    function updateGroupMemberCount() {
        const count = Object.keys(selectedGroupMembers).length;
        if (groupMemberCount) {
            groupMemberCount.textContent = `${count} member${count !== 1 ? 's' : ''} selected`;
        }
        if (groupCreateBtn) {
            groupCreateBtn.disabled = count < 2;
        }
    }

    async function createGroupChat() {
        const name = groupNameInput?.value?.trim();
        const participantIds = Object.keys(selectedGroupMembers);

        if (!name) {
            alert('Please enter a group name.');
            groupNameInput?.focus();
            return;
        }
        if (participantIds.length < 2) {
            alert('Please select at least 2 members for a group chat.');
            return;
        }

        groupCreateBtn.disabled = true;
        groupCreateBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Creating...';

        try {
            const res = await fetch('/api/messages/conversations/group', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ groupName: name, participantIds })
            });

            if (!res.ok) {
                const err = await res.text();
                console.error('Failed to create group:', err);
                alert('Failed to create group chat. ' + (err || 'Please try again.'));
                return;
            }

            const data = await res.json();
            if (data.conversationId) {
                // Switch to chats tab
                tabChats?.click();
                await loadConversations();
                openConversation(data.conversationId, data.groupName, data.initials, true, currentUserId);
            }
        } catch (err) {
            console.error('Failed to create group:', err);
            alert('Failed to create group chat. Please try again.');
        } finally {
            groupCreateBtn.disabled = false;
            groupCreateBtn.innerHTML = '<i class="fas fa-plus-circle"></i> Create Group';
        }
    }

    // ── Group Settings ─────────────────────────────────────────────────
    function setupGroupSettings() {
        // Open drawer
        groupSettingsBtn?.addEventListener('click', () => {
            if (!currentConversationId || !currentConvIsGroup) return;
            groupSettingsDrawer.style.display = 'flex';
            loadGroupDetails(currentConversationId);
        });

        // Close drawer
        groupSettingsBackBtn?.addEventListener('click', () => {
            groupSettingsDrawer.style.display = 'none';
        });

        // Save renamed group
        groupRenameSaveBtn?.addEventListener('click', () => renameCurrentGroup());
        groupRenameInput?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                renameCurrentGroup();
            }
        });

        // Leave group
        groupLeaveBtn?.addEventListener('click', leaveCurrentGroup);
    }

    async function loadGroupDetails(convId) {
        if (!groupSettingsMembers) return;
        groupSettingsMembers.innerHTML = '<div class="messenger-loading"><div class="spinner"></div></div>';

        try {
            const res = await fetch(`/api/messages/conversations/${convId}/details`);
            if (!res.ok) {
                groupSettingsMembers.innerHTML = '<p style="color:red;padding:10px;">Failed to load details.</p>';
                return;
            }
            const data = await res.json();

            // Populate rename input
            if (groupRenameInput) groupRenameInput.value = data.groupName || '';

            // Update member count
            if (groupSettingsMemberCount) {
                groupSettingsMemberCount.textContent = `(${data.members.length})`;
            }

            // Update createdBy from server (in case stale)
            currentConvCreatedBy = data.createdByUserId;

            // Render members
            groupSettingsMembers.innerHTML = '';
            data.members.forEach(m => {
                const memberEl = document.createElement('div');
                memberEl.className = 'group-settings-member';

                const isCreator = m.userId === data.createdByUserId;
                const canRemove = data.isCreator && m.userId !== currentUserId;
                const isSelf = m.userId === currentUserId;

                let badges = '';
                if (isCreator) badges += '<span class="group-member-badge creator">Creator</span>';
                if (isSelf) badges += '<span class="group-member-badge you">You</span>';

                let removeBtn = '';
                if (canRemove) {
                    removeBtn = `<button class="group-remove-member-btn" data-user-id="${m.userId}" title="Remove from group"><i class="fas fa-user-minus"></i></button>`;
                }

                memberEl.innerHTML = `
                    <div class="messenger-people-avatar" style="width:36px;height:36px;font-size:.7rem;">${escapeHtml(m.initials)}</div>
                    <div class="group-settings-member-info">
                        <div class="group-settings-member-name">${escapeHtml(m.name)} ${badges}</div>
                        <div class="group-settings-member-role">${m.department ? escapeHtml(m.department) : ''}${m.position ? ' · ' + escapeHtml(m.position) : ''}</div>
                    </div>
                    ${removeBtn}`;

                // Attach remove handler
                const rmBtn = memberEl.querySelector('.group-remove-member-btn');
                if (rmBtn) {
                    rmBtn.addEventListener('click', () => removeMemberFromGroup(convId, m.userId, m.name));
                }

                groupSettingsMembers.appendChild(memberEl);
            });
        } catch (err) {
            console.error('Failed to load group details:', err);
            groupSettingsMembers.innerHTML = '<p style="color:red;padding:10px;">Error loading details.</p>';
        }
    }

    async function renameCurrentGroup() {
        if (!currentConversationId) return;
        const newName = groupRenameInput?.value?.trim();
        if (!newName) {
            alert('Group name cannot be empty.');
            groupRenameInput?.focus();
            return;
        }

        try {
            const res = await fetch(`/api/messages/conversations/${currentConversationId}/rename`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ groupName: newName })
            });

            if (!res.ok) {
                const err = await res.text();
                alert('Failed to rename group. ' + (err || ''));
                return;
            }

            const data = await res.json();
            // Update header
            chatHeaderName.textContent = data.groupName;
            chatHeaderAvatar.textContent = data.groupName.length >= 2
                ? data.groupName.substring(0, 2).toUpperCase()
                : data.groupName.toUpperCase();

            await loadConversations();
        } catch (err) {
            console.error('Failed to rename group:', err);
            alert('Failed to rename group. Please try again.');
        }
    }

    async function leaveCurrentGroup() {
        if (!currentConversationId) return;
        if (!(await showConfirmModal('Are you sure you want to leave this group?'))) return;

        try {
            const res = await fetch(`/api/messages/conversations/${currentConversationId}/leave`, {
                method: 'POST'
            });

            if (!res.ok) {
                const err = await res.text();
                alert('Failed to leave group. ' + (err || ''));
                return;
            }

            // Close chat view
            chatPlaceholder.style.display = '';
            chatActive.style.display = 'none';
            groupSettingsDrawer.style.display = 'none';
            currentConversationId = null;
            currentConvIsGroup = false;
            currentConvCreatedBy = null;
            sidebar?.classList.remove('hidden-mobile');

            await loadConversations();
            updateNavBadge();
        } catch (err) {
            console.error('Failed to leave group:', err);
            alert('Failed to leave group. Please try again.');
        }
    }

    async function removeMemberFromGroup(convId, userId, userName) {
        if (!(await showConfirmModal('Remove ' + userName + ' from this group?'))) return;

        try {
            const res = await fetch(`/api/messages/conversations/${convId}/members/${encodeURIComponent(userId)}`, {
                method: 'DELETE'
            });

            if (!res.ok) {
                const err = await res.text();
                alert('Failed to remove member. ' + (err || ''));
                return;
            }

            // Refresh group details
            await loadGroupDetails(convId);
            await loadConversations();
        } catch (err) {
            console.error('Failed to remove member:', err);
            alert('Failed to remove member. Please try again.');
        }
    }

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
