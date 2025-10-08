window.irsCulture = {
    // "EN" | "DE" | "HE"
    set(code) {
        const c = (code || "EN").toLowerCase(); // en/de/he
        document.cookie = `.AspNetCore.Culture=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; samesite=lax`;
        document.cookie = `.AspNetCore.Culture=c=${c}|uic=${c}; path=/; samesite=lax; max-age=31536000`;
        try { localStorage.setItem("irs.culture", c); } catch { }
        console.log("[irsCulture] culture ->", c);
    }
};
