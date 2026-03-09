/**
 * Populates the header with the real user name & profile picture.
 * Fetches from /api/ProfileApi/me (cached for the page lifetime).
 */
(function () {
    'use strict';

    async function loadHeaderUser() {
        try {
            const res = await fetch('/api/ProfileApi/me');
            if (!res.ok) return;
            const data = await res.json();
            const p = data.profile;
            if (!p) return;

            const fullName = [p.firstName, p.lastName].filter(Boolean).join(' ');

            // Update header name
            const nameEl = document.getElementById('headerUserName');
            if (nameEl && fullName) nameEl.textContent = fullName;

            // Update header avatar
            const avatarEl = document.getElementById('headerUserAvatar');
            if (avatarEl && p.profileImage) {
                avatarEl.innerHTML = '';
                const img = document.createElement('img');
                img.src = p.profileImage;
                img.alt = fullName;
                img.style.cssText = 'width:100%;height:100%;border-radius:50%;object-fit:cover;';
                avatarEl.appendChild(img);
            } else if (avatarEl && fullName) {
                // Update initials from actual name
                const initials = ((p.firstName || '')[0] || '') + ((p.lastName || '')[0] || '');
                if (initials) avatarEl.textContent = initials.toUpperCase();
            }
        } catch (e) {
            // Silently fail – header keeps defaults
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', loadHeaderUser);
    } else {
        loadHeaderUser();
    }
})();
