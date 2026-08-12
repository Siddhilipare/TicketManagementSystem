document.addEventListener('DOMContentLoaded', function () {
    var root = document.getElementById('dashboardChartsRoot');
    if (!root) return;

    var statusLabels = JSON.parse(root.dataset.statusLabels);
    var statusData = JSON.parse(root.dataset.statusData);
    var priorityLabels = JSON.parse(root.dataset.priorityLabels);
    var priorityData = JSON.parse(root.dataset.priorityData);
    var trendLabels = JSON.parse(root.dataset.trendLabels);
    var trendCreated = JSON.parse(root.dataset.trendCreated);
    var trendResolved = JSON.parse(root.dataset.trendResolved);

    var statusColors = ['#f0ad4e', '#5bc0de', '#9b59b6', '#5cb85c'];   // To Do, In Progress, In Review, Completed
    var priorityColors = ['#5cb85c', '#f0ad4e', '#d9534f', '#95a5a6']; // Low, Medium, High, Unset

    var statusCanvas = document.getElementById('statusChart');
    if (statusCanvas) {
        new Chart(statusCanvas, {
            type: 'doughnut',
            data: {
                labels: statusLabels,
                datasets: [{
                    data: statusData,
                    backgroundColor: statusColors,
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } }
            }
        });
    }

    var priorityCanvas = document.getElementById('priorityChart');
    if (priorityCanvas) {
        new Chart(priorityCanvas, {
            type: 'bar',
            data: {
                labels: priorityLabels,
                datasets: [{
                    label: 'Tickets',
                    data: priorityData,
                    backgroundColor: priorityColors,
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
            }
        });
    }

    var trendCanvas = document.getElementById('trendChart');
    if (trendCanvas) {
        new Chart(trendCanvas, {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: 'Created',
                        data: trendCreated,
                        borderColor: '#5bc0de',
                        backgroundColor: 'rgba(91, 192, 222, 0.15)',
                        tension: 0.3,
                        fill: true
                    },
                    {
                        label: 'Resolved',
                        data: trendResolved,
                        borderColor: '#5cb85c',
                        backgroundColor: 'rgba(92, 184, 92, 0.15)',
                        tension: 0.3,
                        fill: true
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
            }
        });
    }
});