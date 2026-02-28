/**
 * PSGC Cloud API cascading address selects
 * API: https://psgc.cloud/api  (REST, JSON arrays with {code, name})
 *
 * Hierarchy:  Region  →  Province  →  City / Municipality  →  Barangay
 * NCR has no provinces — cities load directly from the region.
 *
 * Exposes:
 *   window.initPSGCSelects()          — (re-)initialise the four <select>s
 *   window.resetPSGCSelects()         — clear all selects to default state
 *   window.getPSGCAddressText()       — { region, province, city, barangay } display names
 */
(function () {
    'use strict';

    const BASE = 'https://psgc.cloud/api';

    /* ── helpers ────────────────────────────────── */

    async function fetchJson(url) {
        try {
            const res = await fetch(url);
            if (!res.ok) throw new Error(res.status + ' ' + res.statusText);
            return await res.json();          // returns an array
        } catch (e) {
            console.warn('[PSGC] fetch failed:', url, e);
            return [];
        }
    }

    function setLoading(select, msg) {
        select.disabled = true;
        select.innerHTML = '<option value="">' + msg + '</option>';
    }

    function populate(select, items, placeholder) {
        const sorted = items.slice().sort((a, b) => a.name.localeCompare(b.name));
        select.innerHTML = '<option value="">' + placeholder + '</option>' +
            sorted.map(i => '<option value="' + i.code + '">' + i.name + '</option>').join('');
        select.disabled = false;
    }

    function resetSelect(select, placeholder) {
        select.innerHTML = '<option value="">' + placeholder + '</option>';
        select.disabled = false;
    }

    function getEl(id) { return document.getElementById(id); }

    /* ── cached region list (loaded once) ──────── */
    let regionCache = null;

    /* ── public API ────────────────────────────── */

    async function init() {
        const regionEl = getEl('psgcRegion');
        const provEl   = getEl('psgcProvince');
        const cityEl   = getEl('psgcCity');
        const brgyEl   = getEl('psgcBarangay');
        if (!regionEl || !provEl || !cityEl || !brgyEl) return;

        // avoid duplicate listeners on re-init
        if (regionEl.dataset.psgcBound) return;
        regionEl.dataset.psgcBound = '1';

        // --- load regions (once) ---
        setLoading(regionEl, 'Loading regions…');
        if (!regionCache) regionCache = await fetchJson(BASE + '/regions');
        populate(regionEl, regionCache, 'Select Region');
        resetSelect(provEl, 'Select Province');
        resetSelect(cityEl, 'Select City / Municipality');
        resetSelect(brgyEl, 'Select Barangay');

        // --- Region change ---
        regionEl.addEventListener('change', async function () {
            const code = this.value;
            resetSelect(cityEl, 'Select City / Municipality');
            resetSelect(brgyEl, 'Select Barangay');

            if (!code) {
                resetSelect(provEl, 'Select Province');
                return;
            }

            // Load provinces for this region
            setLoading(provEl, 'Loading provinces…');
            const provinces = await fetchJson(BASE + '/regions/' + code + '/provinces');

            if (provinces.length === 0) {
                // NCR or regions without provinces — skip province, load cities directly
                resetSelect(provEl, 'N/A for this region');
                provEl.disabled = true;
                setLoading(cityEl, 'Loading cities…');
                const cities = await fetchJson(BASE + '/regions/' + code + '/cities-municipalities');
                populate(cityEl, cities, 'Select City / Municipality');
            } else {
                populate(provEl, provinces, 'Select Province');
            }
        });

        // --- Province change ---
        provEl.addEventListener('change', async function () {
            const code = this.value;
            resetSelect(brgyEl, 'Select Barangay');

            if (!code) {
                resetSelect(cityEl, 'Select City / Municipality');
                return;
            }

            setLoading(cityEl, 'Loading cities…');
            const cities = await fetchJson(BASE + '/provinces/' + code + '/cities-municipalities');
            populate(cityEl, cities, 'Select City / Municipality');
        });

        // --- City change ---
        cityEl.addEventListener('change', async function () {
            const code = this.value;

            if (!code) {
                resetSelect(brgyEl, 'Select Barangay');
                return;
            }

            setLoading(brgyEl, 'Loading barangays…');
            const brgys = await fetchJson(BASE + '/cities-municipalities/' + code + '/barangays');
            populate(brgyEl, brgys, 'Select Barangay');
        });
    }

    function reset() {
        const regionEl = getEl('psgcRegion');
        const provEl   = getEl('psgcProvince');
        const cityEl   = getEl('psgcCity');
        const brgyEl   = getEl('psgcBarangay');
        if (!regionEl) return;

        // Re-populate regions from cache
        if (regionCache) {
            populate(regionEl, regionCache, 'Select Region');
        }
        resetSelect(provEl, 'Select Province');
        resetSelect(cityEl, 'Select City / Municipality');
        resetSelect(brgyEl, 'Select Barangay');
    }

    /** Returns the display-text (names) of the current selections */
    function getAddressText() {
        const text = (id) => {
            const el = getEl(id);
            if (!el || !el.value) return '';
            return el.options[el.selectedIndex]?.text || '';
        };
        return {
            region:   text('psgcRegion'),
            province: text('psgcProvince'),
            city:     text('psgcCity'),
            barangay: text('psgcBarangay')
        };
    }

    // expose
    window.initPSGCSelects    = init;
    window.resetPSGCSelects   = reset;
    window.getPSGCAddressText = getAddressText;

    // auto-init on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
