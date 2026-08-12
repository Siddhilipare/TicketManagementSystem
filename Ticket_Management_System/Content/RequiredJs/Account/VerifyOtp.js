$(document).ready(function () {
    if (window.AssistantBot) { window.AssistantBot.init('#otpForm'); }
    var otpInput = document.getElementById('otp');
    if (otpInput) {
        otpInput.addEventListener('input', function () {
            this.value = this.value.replace(/[^0-9]/g, '').slice(0, 6);
        });
    }
});
