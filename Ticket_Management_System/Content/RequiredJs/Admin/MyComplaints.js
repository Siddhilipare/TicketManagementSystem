function openDeleteModal(id, title, status, date) {
    var delId = document.getElementById('del-id');
    if (delId) delId.textContent = 'TICK-' + String(id).padStart(4, '0');
    var delTitle = document.getElementById('del-title');
    if (delTitle) delTitle.textContent = title;
    var delStatus = document.getElementById('del-status');
    if (delStatus) delStatus.textContent = status;
    var delDate = document.getElementById('del-date');
    if (delDate) delDate.textContent = date;
    var deleteTicketId = document.getElementById('deleteTicketId');
    if (deleteTicketId) deleteTicketId.value = id;
    var deleteForm = document.getElementById('deleteForm');
    if (deleteForm) deleteForm.action = '/Admin/AdminDeleteMyTicket/' + id;
    var deleteModal = document.getElementById('deleteModal');
    if (deleteModal) deleteModal.style.display = 'flex';
}

function closeDeleteModal() {
    var deleteModal = document.getElementById('deleteModal');
    if (deleteModal) deleteModal.style.display = 'none';
}

window.openDeleteModal = openDeleteModal;
window.closeDeleteModal = closeDeleteModal;

document.addEventListener('DOMContentLoaded', function () {
    $(document).on('click', '.open-delete-btn', function () {
        var id = $(this).attr('data-ticket-id') || '';
        var title = $(this).attr('data-title') || '';
        var status = $(this).attr('data-status') || '';
        var date = $(this).attr('data-date') || '';
        openDeleteModal(id, title, status, date);
    });

    var deleteModal = document.getElementById('deleteModal');
    if (deleteModal) {
        deleteModal.addEventListener('click', function (e) {
            if (e.target === this) closeDeleteModal();
        });
    }
});
