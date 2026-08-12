function handleAdminChatFileSelect(input) {
    if (input.files && input.files[0]) {
        var nameEl = document.getElementById('adminChatFilePreviewName');
        if (nameEl) nameEl.textContent = input.files[0].name;
        var badge = document.getElementById('adminChatFilePreviewBadge');
        if (badge) badge.style.display = 'inline-flex';
    }
}

function removeAdminChatFileSelect() {
    var input = document.getElementById('adminChatFileInput');
    if (input) input.value = '';
    var badge = document.getElementById('adminChatFilePreviewBadge');
    if (badge) badge.style.display = 'none';
}

window.handleAdminChatFileSelect = handleAdminChatFileSelect;
window.removeAdminChatFileSelect = removeAdminChatFileSelect;

document.addEventListener('DOMContentLoaded', function () {
    var thread = document.querySelector('.chat-thread');
    if (thread) thread.scrollTop = thread.scrollHeight;
});
