function updateColumnCounts() {
    document.querySelectorAll('[data-status-id]').forEach(function (col) {
        var badge = col.querySelector('.column-count-badge');
        var cardContainer = col.querySelector('.column-cards-container');
        if (badge && cardContainer) {
            var count = cardContainer.querySelectorAll('[data-ticket-id]').length;
            badge.textContent = count;
        }
    });
}

function dragStart(ev) {
    ev.dataTransfer.setData("ticketId", ev.currentTarget.getAttribute("data-ticket-id"));
}

function allowDrop(ev) {
    ev.preventDefault();
}

function dropCard(ev) {
    ev.preventDefault();
    var ticketId = ev.dataTransfer.getData("ticketId");
    var column = ev.currentTarget;
    var statusId = column.getAttribute("data-status-id");
    var card = document.querySelector('[data-ticket-id="' + ticketId + '"]');

    if (card && column) {
        var container = column.querySelector('.column-cards-container') || column;
        container.appendChild(card);
        updateColumnCounts();

        var boardContainer = document.querySelector('.kanban-board-container');
        var updateStatusUrl = (boardContainer && boardContainer.dataset.updateStatusUrl) ? boardContainer.dataset.updateStatusUrl : '/Support/UpdateStatusAjax';

        fetch(updateStatusUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'ticketId=' + ticketId + '&statusId=' + statusId
        })
        .then(function(res) { return res.json(); })
        .then(function(data) {
            if (data.success) {
                if (window.LuxuryUI && window.LuxuryUI.showToast) {
                    LuxuryUI.showToast('success', 'Status Updated', 'Ticket status updated');
                }
            } else {
                if (window.LuxuryUI && window.LuxuryUI.showToast) {
                    LuxuryUI.showToast('error', 'Error', 'Unable to update ticket status');
                }
                location.reload();
            }
        })
        .catch(function(err) {
            if (window.LuxuryUI && window.LuxuryUI.showToast) {
                LuxuryUI.showToast('error', 'Error', 'Connection failed');
            }
        });
    }
}

window.updateColumnCounts = updateColumnCounts;
window.dragStart = dragStart;
window.allowDrop = allowDrop;
window.dropCard = dropCard;

document.addEventListener('DOMContentLoaded', updateColumnCounts);
updateColumnCounts();
