window.irsCharts = {
    upsertPie: function (id, labels, data) {
        var cv = document.getElementById(id);
        if (!cv || typeof Chart === "undefined") return;
        var ctx = cv.getContext('2d');

        var isSmall = window.matchMedia('(max-width: 576px)').matches;
        var opts = {
            responsive: true,
            maintainAspectRatio: false, // we control size via CSS frame
            plugins: {
                legend: { position: isSmall ? 'bottom' : 'right', labels: { boxWidth: 10 } },
                tooltip: { enabled: true }
            },
            layout: { padding: 0 }
        };

        if (!cv._chart) {
            cv._chart = new Chart(ctx, {
                type: 'pie',
                data: { labels: labels || [], datasets: [{ data: data || [] }] },
                options: opts
            });
        } else {
            cv._chart.data.labels = labels || [];
            cv._chart.data.datasets[0].data = data || [];
            cv._chart.options = opts;
            cv._chart.update();
        }
    },
    destroy: function (id) {
        var cv = document.getElementById(id);
        if (cv && cv._chart) { cv._chart.destroy(); cv._chart = null; }
    }
};
