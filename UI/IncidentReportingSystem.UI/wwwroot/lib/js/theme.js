// wwwroot/lib/js/theme.js
window.irsTheme = (function () {
    function apply(mode) {
        // normalize
        var m = (mode === "dark") ? "dark" : "light";
        // reflect on DOM for CSS/Bootstrap and charts
        document.documentElement.setAttribute("data-theme", m);
        document.documentElement.setAttribute("data-bs-theme", m);
        if (document.body) {
            document.body.classList.toggle("dark", m === "dark");
        }
    }

    function set(mode) {
        var m = (mode === "dark") ? "dark" : "light";
        try { localStorage.setItem("irs.theme", m); } catch { }
        apply(m);
    }

    function get() {
        try { return localStorage.getItem("irs.theme") || "light"; }
        catch { return "light"; }
    }

    function toggle() { set(get() === "dark" ? "light" : "dark"); }

    // initialize on load
    try { apply(get()); } catch { }

    return { get, set, toggle };
})();

// keep as-is
window.irsUi = window.irsUi || {};
window.irsUi.offcanvasHide = function (selector) {
    try {
        var el = document.querySelector(selector);
        if (!el || !window.bootstrap) return;
        var inst = bootstrap.Offcanvas.getOrCreateInstance(el);
        inst.hide();
    } catch { }
};
