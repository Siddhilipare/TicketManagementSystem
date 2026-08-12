function handleChatFileSelect(input) {
    if (input.files && input.files[0]) {
        var nameEl = document.getElementById('chatFilePreviewName');
        if (nameEl) nameEl.textContent = input.files[0].name;
        var badge = document.getElementById('chatFilePreviewBadge');
        if (badge) badge.style.display = 'inline-flex';
    }
}

function removeChatFileSelect() {
    var input = document.getElementById('chatFileInput');
    if (input) input.value = '';
    var badge = document.getElementById('chatFilePreviewBadge');
    if (badge) badge.style.display = 'none';
}

window.handleChatFileSelect = handleChatFileSelect;
window.removeChatFileSelect = removeChatFileSelect;

document.addEventListener('DOMContentLoaded', function () {
    var thread = document.querySelector('.chat-thread');
    if (thread) thread.scrollTop = thread.scrollHeight;
});
