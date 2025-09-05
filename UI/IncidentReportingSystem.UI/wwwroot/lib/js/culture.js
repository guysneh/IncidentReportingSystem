// Set/Get culture cookie used by ASP.NET Core RequestLocalization
window.irsCulture = {
    set: function (culture) {
        var d = new Date();
        d.setFullYear(d.getFullYear() + 1);
        // cookie format: c=<culture>|uic=<culture>
        document.cookie = ".AspNetCore.Culture=c=" + culture + "|uic=" + culture +
            "; path=/; expires=" + d.toUTCString();
    },
    get: function () {
        var m = document.cookie.match(/(?:^|; )\.AspNetCore\.Culture=([^;]+)/);
        return m ? decodeURIComponent(m[1]) : null;
    }
};
