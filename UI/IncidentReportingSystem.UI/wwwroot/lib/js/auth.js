// Robust, no-throw auth storage
window.irsAuth = (function () {
    function get() {
        try {
            return JSON.parse(localStorage.getItem("irs.auth") || "{}");
        } catch { return {}; }
    }
    function set(token, expUtc) {
        try {
            localStorage.setItem("irs.auth", JSON.stringify({ token: token || null, expUtc: expUtc || null }));
        } catch { /* no-op */ }
    }
    function clear() {
        try { localStorage.removeItem("irs.auth"); } catch { }
    }
    return { get, set, clear };
})();
