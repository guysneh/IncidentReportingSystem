window.irsRtl = {
    applyByCulture: function (culture) {
        var lang = (culture || 'en').split('-')[0];
        var rtl = (lang === 'he' || lang === 'ar' || lang === 'fa');

        var root = document.documentElement;
        root.lang = lang;
        root.dir = rtl ? 'rtl' : 'ltr';
        root.classList.toggle('rtl', rtl);

        var ltr = document.getElementById('bootstrap-ltr');
        var rtlL = document.getElementById('bootstrap-rtl');
        if (ltr && rtlL) {
            ltr.disabled = rtl;
            rtlL.disabled = !rtl;
        }
    }
};
