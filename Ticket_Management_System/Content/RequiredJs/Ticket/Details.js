function handleEmpChatFileSelect(input) {
    if (input.files && input.files[0]) {
        var nameEl = document.getElementById('empChatFilePreviewName');
        if (nameEl) nameEl.textContent = input.files[0].name;
        var badge = document.getElementById('empChatFilePreviewBadge');
        if (badge) badge.style.display = 'inline-flex';
    }
}

function removeEmpChatFileSelect() {
    var input = document.getElementById('empChatFileInput');
    if (input) input.value = '';
    var badge = document.getElementById('empChatFilePreviewBadge');
    if (badge) badge.style.display = 'none';
}

window.handleEmpChatFileSelect = handleEmpChatFileSelect;
window.removeEmpChatFileSelect = removeEmpChatFileSelect;

document.addEventListener('DOMContentLoaded', function () {
    var thread = document.querySelector('.chat-thread');
    if (thread) thread.scrollTop = thread.scrollHeight;
});
