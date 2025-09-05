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

window.irsUi = window.irsUi || {};
window.irsUi.offcanvasHide = function (selector) {
    try {
        var el = document.querySelector(selector);
        if (!el || !window.bootstrap) return;
        var inst = bootstrap.Offcanvas.getOrCreateInstance(el);
        inst.hide();
    } catch { }
};
