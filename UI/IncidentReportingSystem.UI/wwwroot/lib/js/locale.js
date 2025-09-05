window.irsLocale = {
    apply: function (culture) {
        var lang = (culture || 'en').split('-')[0];
        var rtl = (lang === 'he' || lang === 'ar' || lang === 'fa');
        var root = document.documentElement;
        root.lang = lang;
        root.dir = rtl ? 'rtl' : 'ltr';
    }
};
