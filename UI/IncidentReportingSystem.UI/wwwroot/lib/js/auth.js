(function () {
    const KEY = 'irs.auth';
    function read() {
        try { return JSON.parse(localStorage.getItem(KEY) || 'null'); }
        catch { return null; }
    }
    window.irsAuth = {
        set: function (token, expUtc) {
            localStorage.setItem(KEY, JSON.stringify({ token, expUtc }));
        },
        get: function () { return read(); },
        clear: function () { localStorage.removeItem(KEY); }
    };
})();
