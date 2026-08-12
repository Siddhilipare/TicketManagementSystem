var processingIds = {};

function getUserRole() {
    var card = document.querySelector('.luxury-card[data-user-role]');
    return card ? card.dataset.userRole : '';
}

function getTicketUrl(ticketId) {
    if (!ticketId) return null;
    var role = getUserRole();
    if (role === 'Administrator') return '/Admin/ManageTicket/' + ticketId;
    if (role === 'Support Executive') return '/Support/Details/' + ticketId;
    return '/Ticket/Details/' + ticketId;
}

function handleNotificationClick(notifId, ticketId) {
    var item = document.querySelector('[data-notif-id="' + notifId + '"]');

    if (item && item.classList.contains('read')) return;

    if (processingIds[notifId]) return;
    processingIds[notifId] = true;

    if (item) {
        item.classList.remove('unread');
        item.classList.add('read');
        var msgText = item.querySelector('.notif-message-text');
        if (msgText) { msgText.style.color = 'var(--text-secondary)'; msgText.style.fontWeight = '400'; }
        var timeText = item.querySelector('.notif-time-text');
        if (timeText) { timeText.style.color = 'var(--text-tertiary)'; timeText.style.fontWeight = '400'; }
        var badge = item.querySelector('.badge-unread-pill');
        if (badge) badge.remove();
        var dot = item.querySelector('.unread-dot');
        if (dot) dot.remove();
        var navBadge = document.querySelector('.notif-badge');
        if (navBadge) {
            var count = parseInt(navBadge.textContent) - 1;
            if (count > 0) { navBadge.textContent = count; } else { navBadge.remove(); }
        }
    }

    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenInput ? tokenInput.value : '';

    $.ajax({
        type: 'POST',
        url: '/Notification/MarkAsRead',
        data: {
            id: notifId,
            __RequestVerificationToken: token
        },
        complete: function () {
            delete processingIds[notifId];
        }
    });
}

function markAllNotificationsReadPage() {
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenInput ? tokenInput.value : '';

    $.ajax({
        type: 'POST',
        url: '/Notification/MarkAllAsRead',
        data: { __RequestVerificationToken: token },
        success: function (res) {
            if (res.success) { location.reload(); }
        }
    });
}

window.handleNotificationClick = handleNotificationClick;
window.markAllNotificationsReadPage = markAllNotificationsReadPage;
