(function () {
    const KEY = 'irs.auth';

    function toMs(d) {
        if (!d) return 0;
        if (typeof d === 'number') return d;
        if (typeof d === 'string') {
            const t = Date.parse(d);
            return isNaN(t) ? 0 : t;
        }
        if (d instanceof Date) return d.getTime();
        return 0;
    }

    window.irsAuth = {
        /** שומר JSON כמו { t, exp } */
        set: function (token, expiresAt) {
            try {
                const expMs = toMs(expiresAt);
                const blob = { t: token || '', exp: expMs || 0 };
                localStorage.setItem(KEY, JSON.stringify(blob));
                console.log('[irsAuth.set]', blob);
            } catch (e) { console.warn('irsAuth.set error', e); }
        },
        /** מחזיר את ה-string הגולמי (ל-C# יש deserialize משלו) */
        getRaw: function () {
            const raw = localStorage.getItem(KEY);
            console.log('[irsAuth.getRaw]', raw);
            return raw;
        },
        clear: function () {
            try { localStorage.removeItem(KEY); console.log('[irsAuth.clear]'); } catch { }
        }
    };
})();
