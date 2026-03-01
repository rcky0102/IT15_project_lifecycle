/**
 * Notification Dropdown Module
 * Shared across all role-based layout areas.
 * 
 * Usage: call initNotifications() after the DOM is ready.
 * The bell button must have id="notifBellBtn" and the dropdown container id="notifDropdown".
 */
(function () {
    'use strict';

    const POLL_INTERVAL = 30000; // 30 seconds
    let _pollTimer = null;

    window.initNotifications = function () {
        const bellBtn   = document.getElementById('notifBellBtn');
        const dropdown  = document.getElementById('notifDropdown');
        const badge     = document.getElementById('notifBadge');
        const dot       = document.getElementById('notifDot');
        const listEl    = document.getElementById('notifList');
        const emptyEl   = document.getElementById('notifEmpty');
        const markAllBtn = document.getElementById('notifMarkAllRead');

        if (!bellBtn || !dropdown) return;

        /* ---------- Toggle dropdown ---------- */
        bellBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            const isOpen = dropdown.classList.contains('open');
            if (isOpen) {
                closeDropdown();
            } else {
                openDropdown();
            }
        });

        document.addEventListener('click', function (e) {
            if (!dropdown.contains(e.target) && !bellBtn.contains(e.target)) {
                closeDropdown();
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeDropdown();
        });

        function openDropdown() {
            dropdown.classList.add('open');
            bellBtn.setAttribute('aria-expanded', 'true');
            loadNotifications();
        }

        function closeDropdown() {
            dropdown.classList.remove('open');
            bellBtn.setAttribute('aria-expanded', 'false');
        }

        /* ---------- Load notifications ---------- */
        async function loadNotifications() {
            try {
                const res = await fetch('/api/notifications?count=20', { credentials: 'same-origin' });
                if (!res.ok) return;
                const data = await res.json();
                renderList(data);
            } catch (err) {
                console.error('Failed to load notifications', err);
            }
        }

        /* ---------- Poll unread count ---------- */
        async function pollUnreadCount() {
            try {
                const res = await fetch('/api/notifications/unread-count', { credentials: 'same-origin' });
                if (!res.ok) return;
                const data = await res.json();
                updateBadge(data.count);
            } catch { /* ignore */ }
        }

        function updateBadge(count) {
            if (badge) {
                badge.textContent = count > 99 ? '99+' : count;
                badge.style.display = count > 0 ? 'flex' : 'none';
            }
            if (dot) {
                dot.style.display = count > 0 ? 'block' : 'none';
            }
        }

        /* ---------- Render list ---------- */
        function renderList(notifications) {
            if (!listEl) return;
            listEl.innerHTML = '';

            const unread = notifications.filter(n => !n.isRead).length;
            updateBadge(unread);

            if (notifications.length === 0) {
                if (emptyEl) emptyEl.style.display = 'flex';
                if (markAllBtn) markAllBtn.style.display = 'none';
                return;
            }

            if (emptyEl) emptyEl.style.display = 'none';
            if (markAllBtn) markAllBtn.style.display = unread > 0 ? 'inline-block' : 'none';

            notifications.forEach(function (n) {
                const item = document.createElement('div');
                item.className = 'notif-item' + (n.isRead ? '' : ' unread');
                item.setAttribute('data-id', n.id);

                const iconClass = n.icon || getDefaultIcon(n.type);
                const typeClass = 'notif-icon-' + (n.type || 'Info').toLowerCase();

                item.innerHTML =
                    '<div class="notif-icon-wrap ' + typeClass + '">' +
                        '<i class="' + escHtml(iconClass) + '"></i>' +
                    '</div>' +
                    '<div class="notif-body">' +
                        '<div class="notif-title">' + escHtml(n.title) + '</div>' +
                        '<div class="notif-msg">' + escHtml(n.message) + '</div>' +
                        '<div class="notif-time"><i class="fas fa-clock"></i> ' + escHtml(n.timeAgo) + '</div>' +
                    '</div>' +
                    (!n.isRead ? '<span class="notif-unread-dot"></span>' : '');

                item.addEventListener('click', function () {
                    markAsRead(n.id, item);
                    if (n.link) {
                        window.location.href = n.link;
                    }
                });

                listEl.appendChild(item);
            });
        }

        /* ---------- Mark as read ---------- */
        async function markAsRead(id, itemEl) {
            try {
                await fetch('/api/notifications/' + id + '/read', {
                    method: 'POST',
                    credentials: 'same-origin'
                });
                if (itemEl) {
                    itemEl.classList.remove('unread');
                    var dot = itemEl.querySelector('.notif-unread-dot');
                    if (dot) dot.remove();
                }
                pollUnreadCount();
            } catch { /* ignore */ }
        }

        /* ---------- Mark all as read ---------- */
        if (markAllBtn) {
            markAllBtn.addEventListener('click', async function (e) {
                e.stopPropagation();
                try {
                    await fetch('/api/notifications/read-all', {
                        method: 'POST',
                        credentials: 'same-origin'
                    });
                    document.querySelectorAll('.notif-item.unread').forEach(function (el) {
                        el.classList.remove('unread');
                        var d = el.querySelector('.notif-unread-dot');
                        if (d) d.remove();
                    });
                    updateBadge(0);
                    if (markAllBtn) markAllBtn.style.display = 'none';
                } catch { /* ignore */ }
            });
        }

        /* ---------- Helpers ---------- */
        function getDefaultIcon(type) {
            switch ((type || '').toLowerCase()) {
                case 'success': return 'fas fa-check-circle';
                case 'warning': return 'fas fa-exclamation-triangle';
                case 'error':   return 'fas fa-times-circle';
                default:        return 'fas fa-info-circle';
            }
        }

        function escHtml(str) {
            if (!str) return '';
            var d = document.createElement('div');
            d.appendChild(document.createTextNode(str));
            return d.innerHTML;
        }

        /* ---------- Start polling ---------- */
        pollUnreadCount();
        if (_pollTimer) clearInterval(_pollTimer);
        _pollTimer = setInterval(pollUnreadCount, POLL_INTERVAL);
    };
})();
