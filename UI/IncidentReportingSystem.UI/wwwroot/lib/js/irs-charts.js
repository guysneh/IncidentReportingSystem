window.irsCharts = {
    upsertLine: function (canvasId, labels, values, opts) {
        try {
            this._charts = this._charts || {};
            const el = document.getElementById(canvasId);
            if (!el) return;

            const existing = this._charts[canvasId];
            if (existing) {
                existing.data.labels = labels;
                existing.data.datasets[0].data = values;
                existing.update();
                return;
            }

            const ctx = el.getContext('2d');
            this._charts[canvasId] = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [{
                        label: (opts && opts.label) || 'Count',
                        data: values,
                        tension: 0.3,
                        pointRadius: 3,
                        fill: false
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        x: { ticks: { autoSkip: true, maxTicksLimit: 12 } },
                        y: { beginAtZero: true }
                    }
                }
            });
        } catch (e) { console.error(e); }
    },

    destroy: function (canvasId) {
        try {
            if (this._charts && this._charts[canvasId]) {
                this._charts[canvasId].destroy();
                delete this._charts[canvasId];
            }
        } catch (e) { console.error(e); }
    },

    downloadCsv: function (fileName, csvText) {
        try {
            const blob = new Blob([csvText], { type: "text/csv;charset=utf-8;" });
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url; a.download = fileName;
            document.body.appendChild(a); a.click(); document.body.removeChild(a);
            URL.revokeObjectURL(url);
        } catch (e) { console.error(e); }
    }
};
