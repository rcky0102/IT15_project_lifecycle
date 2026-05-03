/**
 * Profile page logic
 * Fetches user data from /api/ProfileApi/me
 * Updates via  POST /api/ProfileApi/update  (FormData)
 */
(function () {
    'use strict';

    /* ── cache DOM ──────────────────────────────── */
    const $ = (sel) => document.querySelector(sel);
    const viewMode = $('#viewMode');
    const editMode = $('#editMode');
    const editToggleBtn = $('#editToggleBtn');
    const cancelEditBtn = $('#cancelEditBtn');
    const saveProfileBtn = $('#saveProfileBtn');
    const alertBox = $('#profAlert');
    const alertText = $('#profAlertText');
    const alertErrorBox = $('#profAlertError');
    const alertErrorText = $('#profAlertErrorText');

    let profileData = null;  // last fetched data

    /* ── helpers ────────────────────────────────── */
    function val(v) { return v || ''; }
    function display(v, fallback) { return v ? v : (fallback || '—'); }

    function showAlert(msg) {
        alertText.textContent = msg;
        alertBox.style.display = 'flex';
        alertErrorBox.style.display = 'none';
        setTimeout(() => alertBox.style.display = 'none', 5000);
    }
    function showError(msg) {
        alertErrorText.textContent = msg;
        alertErrorBox.style.display = 'flex';
        alertBox.style.display = 'none';
    }

    function initials(first, last) {
        return ((first || '')[0] || '') + ((last || '')[0] || '') || '??';
    }

    /* ── fetch profile ─────────────────────────── */
    async function loadProfile() {
        try {
            const res = await fetch('/api/ProfileApi/me');
            if (!res.ok) throw new Error('Failed to load');
            const data = await res.json();
            profileData = data;
            render(data);
        } catch (e) {
            console.error(e);
            showError('Unable to load profile data.');
        }
    }

    /* ── render view mode ──────────────────────── */
    function render(data) {
        const p = data.profile;
        const fullName = [p.firstName, p.middleName, p.lastName].filter(Boolean).join(' ');

        // Hero
        $('#heroName').textContent = fullName;
        $('#heroRole').textContent = data.role;
        $('#heroEmail').textContent = data.email;
        $('#heroEmpNo').textContent = p.employeeNumber;
        $('#heroInitials').textContent = initials(p.firstName, p.lastName);

        if (p.profileImage) {
            $('#heroImg').src = p.profileImage;
            $('#heroImg').style.display = 'block';
            $('#heroInitials').style.display = 'none';
        } else {
            $('#heroImg').style.display = 'none';
            $('#heroInitials').style.display = '';
        }

        // View fields
        setText('vFirstName', p.firstName);
        setText('vMiddleName', p.middleName);
        setText('vLastName', p.lastName);
        setText('vEmail', data.email);
        setText('vEmpNo', p.employeeNumber);
        setText('vDepartment', p.departmentName);
        setText('vPosition', p.positionName);
        setText('vRegion', p.region);
        setText('vProvince', p.province);
        setText('vCity', p.city);
        setText('vBarangay', p.barangay);
        setText('vAddressLine', p.addressLine);

        // Contact — hide row for Employee (no contact field)
        if (p.contact !== undefined && p.contact !== null) {
            setText('vContact', p.contact);
            if ($('#vContactRow')) $('#vContactRow').style.display = '';
            if ($('#eContactRow')) $('#eContactRow').style.display = '';
        } else {
            if ($('#vContactRow')) $('#vContactRow').style.display = 'none';
            if ($('#eContactRow')) $('#eContactRow').style.display = 'none';
        }

        // Date hired — only for Employee
        if (p.dateHired) {
            const d = new Date(p.dateHired);
            setText('vDateHired', d.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' }));
            if ($('#vDateHiredRow')) $('#vDateHiredRow').style.display = '';
        } else {
            if ($('#vDateHiredRow')) $('#vDateHiredRow').style.display = 'none';
        }

        // Handle SuperAdmin specific visibility
        if (data.role === 'SuperAdmin') {
            // Hide Address section in View Mode
            const addressCard = $('#vRegion')?.closest('.col-12');
            if (addressCard) addressCard.style.display = 'none';

            // Hide Organization section in View Mode
            const orgCard = $('#vEmpNo')?.closest('.col-lg-6');
            if (orgCard) orgCard.style.display = 'none';

            // Hide Address section in Edit Mode
            const editAddressCard = $('#psgcRegion')?.closest('.col-12');
            if (editAddressCard) editAddressCard.style.display = 'none';

            // Hide Hero Employee Number
            const heroEmpNo = $('.prof-hero-emp-no');
            if (heroEmpNo) heroEmpNo.style.display = 'none';
        } else {
            // Show them if they were hidden (e.g. if switching accounts without refresh, though unlikely)
            const addressCard = $('#vRegion')?.closest('.col-12');
            if (addressCard) addressCard.style.display = '';
            const orgCard = $('#vEmpNo')?.closest('.col-lg-6');
            if (orgCard) orgCard.style.display = '';
            const editAddressCard = $('#psgcRegion')?.closest('.col-12');
            if (editAddressCard) editAddressCard.style.display = '';
            const heroEmpNo = $('.prof-hero-emp-no');
            if (heroEmpNo) heroEmpNo.style.display = '';
        }
    }

    function setText(id, v) {
        const el = document.getElementById(id);
        if (!el) return;
        el.textContent = display(v);
        el.classList.toggle('empty', !v);
    }

    /* ── populate edit form ────────────────────── */
    function populateEdit(data) {
        const p = data.profile;
        setInput('eFirstName', p.firstName);
        setInput('eMiddleName', p.middleName);
        setInput('eLastName', p.lastName);
        setInput('eContact', p.contact);
        setInput('eEmail', data.email);
        setInput('eAddressLine', p.addressLine);
        setInput('eCurrentPassword', '');
        setInput('eNewPassword', '');

        // Image
        if (p.profileImage) {
            const prev = $('#editImgPreview');
            if (prev) { prev.src = p.profileImage; prev.style.display = 'block'; }
            const ph = $('#editImgPlaceholder');
            if (ph) ph.style.display = 'none';
        } else {
            const prev = $('#editImgPreview');
            if (prev) prev.style.display = 'none';
            const ph = $('#editImgPlaceholder');
            if (ph) ph.style.display = '';
        }

        // Reset file input
        const fi = $('#editImageInput');
        if (fi) fi.value = '';
        const ai = $('#avatarInput');
        if (ai) ai.value = '';

        // PSGC — init selects
        if (window.initPSGCSelects) {
            // Remove bound flag so it re-initialises
            const regionEl = document.getElementById('psgcRegion');
            if (regionEl) delete regionEl.dataset.psgcBound;
            window.initPSGCSelects();
        }
    }

    function setInput(id, v) {
        const el = document.getElementById(id);
        if (el) el.value = val(v);
    }

    /* ── toggle modes ──────────────────────────── */
    function enterEdit() {
        populateEdit(profileData);
        viewMode.style.display = 'none';
        editMode.style.display = '';
        editToggleBtn.innerHTML = '<i class="fas fa-eye"></i> <span>View Profile</span>';
        $('#avatarEditLabel').style.display = 'flex';
    }
    function exitEdit() {
        viewMode.style.display = '';
        editMode.style.display = 'none';
        editToggleBtn.innerHTML = '<i class="fas fa-pen"></i> <span>Edit Profile</span>';
        $('#avatarEditLabel').style.display = 'none';
    }

    editToggleBtn?.addEventListener('click', () => {
        if (editMode.style.display === 'none') enterEdit();
        else exitEdit();
    });
    cancelEditBtn?.addEventListener('click', exitEdit);

    /* ── save profile ──────────────────────────── */
    // Track email availability state
    let emailAvailable = true;

    // Check email on blur
    $('#eEmail')?.addEventListener('blur', async function () {
        const email = (this.value || '').trim();
        const errEl = document.getElementById('eEmailError');
        if (!email) {
            errEl.style.display = 'none';
            emailAvailable = true;
            return;
        }
        try {
            const resp = await fetch('/api/ProfileApi/check-email?email=' + encodeURIComponent(email));
            if (resp.ok) {
                const j = await resp.json();
                if (!j.available) {
                    errEl.textContent = 'This email is already used by another account.';
                    errEl.style.display = 'block';
                    emailAvailable = false;
                } else {
                    errEl.style.display = 'none';
                    emailAvailable = true;
                }
            } else {
                errEl.style.display = 'none';
                emailAvailable = true;
            }
        } catch (e) {
            errEl.style.display = 'none';
            emailAvailable = true;
        }
    });

    saveProfileBtn?.addEventListener('click', async () => {
        const firstName = val($('#eFirstName')?.value).trim();
        const lastName = val($('#eLastName')?.value).trim();

        if (!firstName || !lastName) {
            showError('First name and last name are required.');
            return;
        }

        if (!emailAvailable) {
            showError('Please fix the email address before saving.');
            return;
        }

        const fd = new FormData();
        fd.append('FirstName', firstName);
        fd.append('MiddleName', val($('#eMiddleName')?.value).trim());
        fd.append('LastName', lastName);
        fd.append('Contact', val($('#eContact')?.value).trim());
        fd.append('Email', val($('#eEmail')?.value).trim());
        fd.append('CurrentPassword', val($('#eCurrentPassword')?.value));
        fd.append('NewPassword', val($('#eNewPassword')?.value));

        // Address from PSGC selects (text names)
        if (window.getPSGCAddressText) {
            const addr = window.getPSGCAddressText();
            fd.append('Region', addr.region || '');
            fd.append('Province', addr.province || '');
            fd.append('City', addr.city || '');
            fd.append('Barangay', addr.barangay || '');
        }
        fd.append('AddressLine', val($('#eAddressLine')?.value).trim());

        // Profile image — prefer edit form file, fallback to hero avatar input
        const editFile = $('#editImageInput')?.files?.[0];
        const avatarFile = $('#avatarInput')?.files?.[0];
        const file = editFile || avatarFile;
        if (file) fd.append('ProfileImageFile', file);

        saveProfileBtn.disabled = true;
        saveProfileBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/api/ProfileApi/update', {
                method: 'POST',
                body: fd,
                headers: token ? { 'RequestVerificationToken': token } : {}
            });
            const result = await res.json();

            if (res.ok && result.success) {
                showAlert(result.message || 'Profile updated successfully.');
                await loadProfile();
                exitEdit();
            } else {
                const msgs = result.errors?.join(', ') || 'Update failed.';
                showError(msgs);
            }
        } catch (e) {
            console.error(e);
            showError('Network error. Please try again.');
        } finally {
            saveProfileBtn.disabled = false;
            saveProfileBtn.innerHTML = '<i class="fas fa-save"></i> Save Changes';
        }
    });

    /* ── image preview (edit form) ─────────────── */
    $('#editImageInput')?.addEventListener('change', function () {
        previewFile(this.files?.[0], 'editImgPreview', 'editImgPlaceholder');
    });

    /* ── image preview (hero avatar camera) ────── */
    $('#avatarInput')?.addEventListener('change', function () {
        const file = this.files?.[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = (e) => {
            const img = $('#heroImg');
            if (img) { img.src = e.target.result; img.style.display = 'block'; }
            const init = $('#heroInitials');
            if (init) init.style.display = 'none';
        };
        reader.readAsDataURL(file);
        // Also reflect in edit form preview
        previewFile(file, 'editImgPreview', 'editImgPlaceholder');
    });

    function previewFile(file, previewId, placeholderId) {
        if (!file) return;
        const reader = new FileReader();
        reader.onload = (e) => {
            const prev = document.getElementById(previewId);
            if (prev) { prev.src = e.target.result; prev.style.display = 'block'; }
            const ph = document.getElementById(placeholderId);
            if (ph) ph.style.display = 'none';
        };
        reader.readAsDataURL(file);
    }

    /* ── password toggle ───────────────────────── */
    document.querySelectorAll('.prof-pw-toggle').forEach(btn => {
        btn.addEventListener('click', () => {
            const input = btn.previousElementSibling;
            if (!input) return;
            const isPw = input.type === 'password';
            input.type = isPw ? 'text' : 'password';
            btn.innerHTML = isPw ? '<i class="fas fa-eye-slash"></i>' : '<i class="fas fa-eye"></i>';
        });
    });

    /* ── init ──────────────────────────────────── */
    loadProfile();

})();
