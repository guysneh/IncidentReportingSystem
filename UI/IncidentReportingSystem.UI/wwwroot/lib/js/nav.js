// ── Edge-swipe for Offcanvas (mobile only) ────────────────────────────────────
window.irsUi = window.irsUi || {};

window.irsUi.enableEdgeSwipe = function (selector, opts) {
    const cfg = Object.assign({ edgeSize: 24, threshold: 40, maxWidth: 768 }, opts || {});
    let activeEdge = null, startX = 0, startY = 0, tracking = false;

    const isMobile = () => window.matchMedia(`(max-width:${cfg.maxWidth}px)`).matches;
    const getOffcanvas = () => {
        const el = document.querySelector(selector);
        if (!el || !window.bootstrap) return null;
        return bootstrap.Offcanvas.getOrCreateInstance(el);
    };

    const onStart = (e) => {
        if (!isMobile()) return;
        const t = e.touches ? e.touches[0] : e;
        startX = t.clientX; startY = t.clientY; tracking = false; activeEdge = null;

        const w = window.innerWidth;
        if (startX <= cfg.edgeSize) activeEdge = "left";
        else if (startX >= w - cfg.edgeSize) activeEdge = "right";
    };

    const onMove = (e) => {
        if (!activeEdge) return;
        const t = e.touches ? e.touches[0] : e;
        const dx = t.clientX - startX;
        const dy = t.clientY - startY;

        if (!tracking && Math.abs(dx) > 10 && Math.abs(dx) > Math.abs(dy)) {
            tracking = true;
        }
        if (tracking) {
            e.preventDefault();
        }
    };

    const onEnd = (e) => {
        if (!activeEdge) return;
        const t = e.changedTouches ? e.changedTouches[0] : e;
        const dx = t.clientX - startX;

        const inward =
            (activeEdge === "left" && dx > cfg.threshold) ||
            (activeEdge === "right" && dx < -cfg.threshold);

        if (inward) {
            const off = getOffcanvas();
            if (off) off.show();
        }
        activeEdge = null; tracking = false;
    };

    window.addEventListener("touchstart", onStart, { passive: true });
    window.addEventListener("touchmove", onMove, { passive: false });
    window.addEventListener("touchend", onEnd, { passive: true });

    const el = document.querySelector(selector);
    if (el) {
        let sx = 0, sy = 0, dragging = false;
        el.addEventListener("touchstart", (e) => {
            if (!isMobile()) return;
            const t = e.touches[0]; sx = t.clientX; sy = t.clientY; dragging = false;
        }, { passive: true });
        el.addEventListener("touchmove", (e) => {
            const t = e.touches[0];
            const dx = t.clientX - sx, dy = t.clientY - sy;
            if (!dragging && Math.abs(dx) > 10 && Math.abs(dx) > Math.abs(dy)) dragging = true;
            if (dragging) e.preventDefault();
        }, { passive: false });
        el.addEventListener("touchend", (e) => {
            const t = e.changedTouches[0];
            const dx = t.clientX - sx;
            if (el.classList.contains("offcanvas-end") && dx > 40) {
                const off = getOffcanvas(); if (off) off.hide();
            }
        }, { passive: true });
    }
};

document.addEventListener("DOMContentLoaded", function () {
    try { window.irsUi.enableEdgeSwipe("#appSideNav"); } catch { }
});
