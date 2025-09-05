window.irsCulture = (function () {
    function set(code) {
        try {
            localStorage.setItem('irs.culture', code);
            document.documentElement.lang = code;
            document.documentElement.dir = (code === 'he' ? 'rtl' : 'ltr');
        } catch { /* no-op */ }
    }
    function get() {
        try { return localStorage.getItem('irs.culture') || 'en'; }
        catch { return 'en'; }
    }
    function applyAndReload(code) {
        set(code);
        // Avoid race with .NET render batch by letting browser schedule the reload
        setTimeout(() => location.replace(location.href), 0);
    }
    try { set(get()); } catch { }
    return { get, set, applyAndReload };
})();
