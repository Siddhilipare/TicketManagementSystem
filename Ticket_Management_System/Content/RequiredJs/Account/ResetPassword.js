$(document).ready(function () {
    if (window.AssistantBot) { window.AssistantBot.init('#resetForm'); }

    var np = document.getElementById('newPwd');
    var cp = document.getElementById('confPwd');
    var msg = document.getElementById('matchMsg');
    function checkMatch() {
        if (!msg) return;
        if (!cp || !cp.value) { msg.textContent = ''; return; }
        if (np && np.value === cp.value) {
            msg.textContent = '\u2713 Passwords match';
            msg.style.color = 'var(--success)';
        } else {
            msg.textContent = '\u2717 Passwords do not match';
            msg.style.color = 'var(--error)';
        }
    }
    if (np && cp) {
        np.addEventListener('input', checkMatch);
        cp.addEventListener('input', checkMatch);
    }
});
