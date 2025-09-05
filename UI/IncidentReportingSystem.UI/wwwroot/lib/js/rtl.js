window.irsRtl = {
    applyByCulture: function (culture) {
        var lang = (culture || 'en').split('-')[0];
        var rtl = (lang === 'he' || lang === 'ar' || lang === 'fa');

        var root = document.documentElement;
        root.lang = lang;
        root.dir = rtl ? 'rtl' : 'ltr';
        root.classList.toggle('rtl', rtl);

        // swap bootstrap css
        var link = document.getElementById('bootstrap');
        if (link) {
            var href = link.getAttribute('href') || '';
            var want = rtl ? 'bootstrap.rtl.min.css' : 'bootstrap.min.css';
            if (!href.endsWith(want)) {
                link.href = href.replace(/bootstrap(\.rtl)?\.min\.css$/, want);
            }
        }
    }
};
