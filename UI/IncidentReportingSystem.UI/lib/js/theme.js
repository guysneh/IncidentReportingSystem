window.irsTheme = (function () {
    function apply(mode) {
        try {
            localStorage.setItem('irs.theme', mode);
            document.documentElement.setAttribute('data-theme', mode);
        } catch (_) { }
    }
    function toggle() { set(get() === 'dark' ? 'light' : 'dark'); }
    function get() { try { return localStorage.getItem('irs.theme') || 'light'; } catch { return 'light'; } }
    function set(m) { apply(m); }
    try { apply(get()); } catch (_) { }
    return { get, set, toggle };
})();
