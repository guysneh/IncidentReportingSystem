// wwwroot/lib/js/irs-charts.js
(function () {
    // single global namespace
    const charts = (window.irsCharts = window.irsCharts || {});
    charts._charts = charts._charts || {};

    function pieColors(n) {
        const out = [];
        const count = Math.max(1, n);
        for (let i = 0; i < n; i++) {
            const h = Math.round((360 * i) / count);
            out.push(`hsl(${h} 70% 55%)`);
        }
        return out;
    }

    charts.upsertLine = function (canvasId, labels, values, opts) {
        try {
            const el = document.getElementById(canvasId);
            if (!el) return;

            const existing = charts._charts[canvasId];
            if (existing) {
                existing.data.labels = labels || [];
                existing.data.datasets[0].data = values || [];
                existing.update();
                return;
            }

            const ctx = el.getContext("2d");
            charts._charts[canvasId] = new Chart(ctx, {
                type: "line",
                data: {
                    labels: labels || [],
                    datasets: [
                        {
                            label: (opts && opts.label) || "",
                            data: values || [],
                            tension: 0.3,
                            pointRadius: 3,
                            fill: false,
                        },
                    ],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        x: { ticks: { autoSkip: true, maxTicksLimit: 12 } },
                        y: { beginAtZero: true },
                    },
                },
            });
        } catch (e) {
            console.error(e);
        }
    };

    charts.upsertPie = function (canvasId, labels, values) {
        try {
            const el = document.getElementById(canvasId);
            if (!el) return;

            const existing = charts._charts[canvasId];
            if (existing) {
                existing.data.labels = labels || [];
                existing.data.datasets[0].data = values || [];
                existing.data.datasets[0].backgroundColor = pieColors(
                    (values || []).length
                );
                existing.update();
                return;
            }

            const ctx = el.getContext("2d");
            charts._charts[canvasId] = new Chart(ctx, {
                type: "pie",
                data: {
                    labels: labels || [],
                    datasets: [
                        {
                            data: values || [],
                            backgroundColor: pieColors((values || []).length),
                            borderWidth: 1,
                        },
                    ],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { position: "bottom" },
                        tooltip: { enabled: true },
                    },
                },
            });
        } catch (e) {
            console.error(e);
        }
    };

    charts.destroy = function (canvasId) {
        try {
            const c = charts._charts && charts._charts[canvasId];
            if (c) {
                c.destroy();
                delete charts._charts[canvasId];
            }
        } catch (e) {
            console.error(e);
        }
    };

    charts.downloadCsv = function (fileName, csvText) {
        try {
            const blob = new Blob([csvText], { type: "text/csv;charset=utf-8;" });
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        } catch (e) {
            console.error(e);
        }
    };

    // helpful log to confirm correct file is loaded
    try {
        console.log("[irsCharts] ready:", Object.keys(charts));
    } catch { }
})();
