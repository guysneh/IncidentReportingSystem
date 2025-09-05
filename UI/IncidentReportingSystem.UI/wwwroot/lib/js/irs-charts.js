window.irsCharts = {
    upsertPie: function (id, labels, data) {
        var cv = document.getElementById(id);
        if (!cv || typeof Chart === "undefined") return;
        var ctx = cv.getContext('2d');
        if (!cv._chart) {
            cv._chart = new Chart(ctx, {
                type: 'pie',
                data: { labels: labels, datasets: [{ data: data }] },
                options: { responsive: true, maintainAspectRatio: false }
            });
        } else {
            cv._chart.data.labels = labels || [];
            var ds = cv._chart.data.datasets[0];
            ds.data = data || [];
            cv._chart.update();
        }
    },
    destroy: function (id) {
        var cv = document.getElementById(id);
        if (cv && cv._chart) { cv._chart.destroy(); cv._chart = null; }
    }
};
