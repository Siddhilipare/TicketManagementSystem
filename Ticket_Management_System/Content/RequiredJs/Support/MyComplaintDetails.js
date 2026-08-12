function handleSupportChatFileSelect(input) {
    if (input.files && input.files[0]) {
        var nameEl = document.getElementById('supportChatFilePreviewName');
        if (nameEl) nameEl.textContent = input.files[0].name;
        var badge = document.getElementById('supportChatFilePreviewBadge');
        if (badge) badge.style.display = 'inline-flex';
    }
}

function removeSupportChatFileSelect() {
    var input = document.getElementById('supportChatFileInput');
    if (input) input.value = '';
    var badge = document.getElementById('supportChatFilePreviewBadge');
    if (badge) badge.style.display = 'none';
}

window.handleSupportChatFileSelect = handleSupportChatFileSelect;
window.removeSupportChatFileSelect = removeSupportChatFileSelect;

document.addEventListener('DOMContentLoaded', function () {
    var thread = document.querySelector('.chat-thread');
    if (thread) thread.scrollTop = thread.scrollHeight;
});
