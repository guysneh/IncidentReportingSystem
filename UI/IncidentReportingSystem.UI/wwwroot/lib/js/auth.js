window.irsAuth = {
    set: function (token, expiresAtUtc) {
        try {
            localStorage.setItem('irs.token', token);
            localStorage.setItem('irs.expiresAtUtc', new Date(expiresAtUtc).toISOString());
        } catch (e) { console.warn('irsAuth.set failed', e); }
    },
    get: function () {
        try {
            const token = localStorage.getItem('irs.token');
            const expRaw = localStorage.getItem('irs.expiresAtUtc');
            if (!token || !expRaw) return null;
            const exp = new Date(expRaw);
            return { token: token, expiresAtUtc: exp.toISOString() };
        } catch (e) { console.warn('irsAuth.get failed', e); return null; }
    },
    clear: function () {
        try {
            localStorage.removeItem('irs.token');
            localStorage.removeItem('irs.expiresAtUtc');
        } catch (e) { console.warn('irsAuth.clear failed', e); }
    }
};
