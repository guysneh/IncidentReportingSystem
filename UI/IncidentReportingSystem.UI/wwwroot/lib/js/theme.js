window.irsTheme = (function () {
    function set(mode) {
        try {
            localStorage.setItem('irs.theme', mode);
            document.documentElement.setAttribute('data-theme', mode);
        } catch { }
    }
    function get() {
        try { return localStorage.getItem('irs.theme') || 'light'; }
        catch { return 'light'; }
    }
    function toggle() { set(get() === 'dark' ? 'light' : 'dark'); }
    try { set(get()); } catch { }
    return { get, set, toggle };
})();
