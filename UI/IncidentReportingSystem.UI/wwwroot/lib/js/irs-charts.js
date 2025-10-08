// wwwroot/lib/js/irs-charts.js
(function () {
    // expose namespace
    const charts = (window.irsCharts = window.irsCharts || {});
    charts._charts = charts._charts || {};

    // ---- helpers ----
    function readVar(name) {
        const root = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
        if (root) return root;
        if (document.body) {
            const body = getComputedStyle(document.body).getPropertyValue(name).trim();
            if (body) return body;
        }
        return "";
    }
    function readTheme() {
        return {
            fore: readVar("--chart-fore") || "#222",
            grid: readVar("--chart-grid") || "rgba(0,0,0,.1)",
            tipBg: readVar("--chart-tip-bg") || "rgba(0,0,0,.82)",
            tipFg: readVar("--chart-tip-fg") || "#fff",
            canvasBg: readVar("--chart-canvas-bg") || "transparent",
            areaBg: readVar("--chart-area-bg") || "transparent"
        };
    }

    // ---- background plugin (canvas & plot area) ----
    const bgPlugin = {
        id: "irsBg",
        beforeDraw(chart) {
            const th = readTheme();
            const cfg = (chart.options?.plugins?.irsBg) || {};
            const canvasColor = (cfg.canvas ?? th.canvasBg);
            const areaColor = (cfg.area ?? th.areaBg);
            const { ctx, canvas, chartArea } = chart;

            if (canvasColor && canvasColor !== "transparent") {
                ctx.save();
                ctx.globalCompositeOperation = "destination-over";
                ctx.fillStyle = canvasColor;
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                ctx.restore();
                // keep DOM style in sync
                canvas.style.backgroundColor = canvasColor;
            }

            if (areaColor && areaColor !== "transparent" && chartArea) {
                ctx.save();
                ctx.fillStyle = areaColor;
                ctx.fillRect(chartArea.left, chartArea.top,
                    chartArea.right - chartArea.left,
                    chartArea.bottom - chartArea.top);
                ctx.restore();
            }
        }
    };
    try { Chart.register(bgPlugin); } catch { /* Chart.js not yet loaded */ }

    // ---- theme applicators ----
    function applyLineLikeTheme(cfg, th) {
        cfg.options = cfg.options || {};
        cfg.options.plugins = cfg.options.plugins || {};
        (cfg.options.plugins.legend ||= {}).labels = { ...(cfg.options.plugins.legend.labels || {}), color: th.fore };

        (cfg.options.plugins.tooltip ||= {});
        cfg.options.plugins.tooltip.backgroundColor = th.tipBg;
        cfg.options.plugins.tooltip.titleColor = th.tipFg;
        cfg.options.plugins.tooltip.bodyColor = th.tipFg;

        cfg.options.plugins.irsBg = { canvas: th.canvasBg, area: th.areaBg };

        (cfg.options.scales ||= {});
        (cfg.options.scales.x ||= {});
        (cfg.options.scales.y ||= {});

        // grid & ticks
        (cfg.options.scales.x.grid ||= {}).color = th.grid;
        (cfg.options.scales.y.grid ||= {}).color = th.grid;
        (cfg.options.scales.x.ticks ||= {}).color = th.fore;
        (cfg.options.scales.y.ticks ||= {}).color = th.fore;

        // NEW: axis frame (left/bottom border)
        (cfg.options.scales.x.border ||= {}).color = th.grid;
        (cfg.options.scales.y.border ||= {}).color = th.grid;
    }


    function applyPieTheme(cfg, th) {
        cfg.options = cfg.options || {};
        cfg.options.plugins = cfg.options.plugins || {};
        (cfg.options.plugins.legend ||= {}).labels = { ...(cfg.options.plugins.legend.labels || {}), color: th.fore };
        (cfg.options.plugins.tooltip ||= {});
        cfg.options.plugins.tooltip.backgroundColor = th.tipBg;
        cfg.options.plugins.tooltip.titleColor = th.tipFg;
        cfg.options.plugins.tooltip.bodyColor = th.tipFg;
        cfg.options.plugins.irsBg = { canvas: th.canvasBg, area: th.areaBg };
    }

    function pieColors(n) {
        const out = [];
        const count = Math.max(1, n | 0);
        for (let i = 0; i < count; i++) {
            const h = Math.round((360 * i) / count);
            out.push(`hsl(${h} 70% 55%)`);
        }
        return out;
    }

    // ---- API ----
    charts.upsertLine = function (canvasId, labels, values, opts) {
        try {
            const el = document.getElementById(canvasId);
            if (!el) return;
            const th = readTheme();
            el.style.backgroundColor = th.canvasBg;

            const existing = charts._charts[canvasId];
            if (existing) {
                existing.data.labels = labels || [];
                existing.data.datasets[0].data = values || [];
                applyLineLikeTheme(existing.config, th);
                existing.update();
                return;
            }

            const cfg = {
                type: "line",
                data: {
                    labels: labels || [],
                    datasets: [{
                        label: (opts && opts.label) || "",
                        data: values || [],
                        tension: 0.3,
                        pointRadius: 3,
                        fill: false
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: true }, irsBg: {} },
                    scales: { x: {}, y: { beginAtZero: true } }
                }
            };
            applyLineLikeTheme(cfg, th);
            charts._charts[canvasId] = new Chart(el.getContext("2d"), cfg);
        } catch (e) { console.error(e); }
    };

    charts.upsertPie = function (canvasId, labels, values) {
        try {
            const el = document.getElementById(canvasId);
            if (!el) return;
            const th = readTheme();
            el.style.backgroundColor = th.canvasBg;

            const existing = charts._charts[canvasId];
            if (existing) {
                existing.data.labels = labels || [];
                existing.data.datasets[0].data = values || [];
                existing.data.datasets[0].backgroundColor = pieColors((values || []).length);
                applyPieTheme(existing.config, th);
                existing.update();
                return;
            }

            const cfg = {
                type: "pie",
                data: {
                    labels: labels || [],
                    datasets: [{
                        data: values || [],
                        backgroundColor: pieColors((values || []).length),
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { position: "bottom" }, tooltip: {}, irsBg: {} }
                }
            };
            applyPieTheme(cfg, th);
            charts._charts[canvasId] = new Chart(el.getContext("2d"), cfg);
        } catch (e) { console.error(e); }
    };

    charts.destroy = function (canvasId) {
        try {
            if (charts._charts && charts._charts[canvasId]) {
                charts._charts[canvasId].destroy();
                delete charts._charts[canvasId];
            }
        } catch (e) { console.error(e); }
    };

    charts.applyTheme = function () {
        try {
            const th = readTheme();
            Object.values(charts._charts || {}).forEach(c => {
                const t = c.config.type;
                if (t === "line" || t === "bar") applyLineLikeTheme(c.config, th);
                else if (t === "pie" || t === "doughnut") applyPieTheme(c.config, th);
                if (c.canvas) c.canvas.style.backgroundColor = th.canvasBg;
                c.update("none");
            });
        } catch (e) { console.error(e); }
    };

    // auto-apply when theme class/attr changes
    try {
        const obs = new MutationObserver(() => charts.applyTheme());
        obs.observe(document.documentElement, { attributes: true, attributeFilter: ["class", "data-theme", "data-bs-theme"] });
        if (document.body) obs.observe(document.body, { attributes: true, attributeFilter: ["class", "data-theme", "data-bs-theme"] });
    } catch { }
})();
