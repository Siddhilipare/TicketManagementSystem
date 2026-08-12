document.addEventListener('DOMContentLoaded', function () {
    var np = document.getElementById('newPassword');
    var cp = document.getElementById('confirmPassword');
    var msg = document.getElementById('matchMsg');
    function checkMatch() {
        if (!cp || !msg) return;
        if (!cp.value) { msg.textContent = ''; return; }
        if (np.value === cp.value) {
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
