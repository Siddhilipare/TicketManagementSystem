document.addEventListener("DOMContentLoaded", function () {
    var form = document.querySelector("form");
    if (!form) return;

    form.addEventListener("submit", function (e) {
        var title = document.getElementById("ticketTitle");
        var desc = document.getElementById("ticketDesc");

        if (title && title.value.trim().length > 0 && title.value.trim().length < 3) {
            var titleErr = document.getElementById("titleErr");
            if (titleErr) titleErr.style.display = "inline";
            title.focus();
            e.preventDefault();
            e.stopPropagation();
            if (e.stopImmediatePropagation) e.stopImmediatePropagation();
            return;
        }
        var titleErrEl = document.getElementById("titleErr");
        if (titleErrEl) titleErrEl.style.display = "none";

        if (desc && desc.value.trim().length > 0 && desc.value.trim().length < 10) {
            var descErr = document.getElementById("descErr");
            if (descErr) descErr.style.display = "inline";
            desc.focus();
            e.preventDefault();
            e.stopPropagation();
            if (e.stopImmediatePropagation) e.stopImmediatePropagation();
            return;
        }
        var descErrEl = document.getElementById("descErr");
        if (descErrEl) descErrEl.style.display = "none";
    }, true);
});
