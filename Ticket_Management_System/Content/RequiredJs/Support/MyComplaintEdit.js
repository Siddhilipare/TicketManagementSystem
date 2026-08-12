function validateFiles(input) {
    var existingCount = parseInt(input.dataset.existingCount || '0', 10);
    var allowed = ['.jpg','.jpeg','.png','.gif','.bmp','.pdf','.txt','.doc','.docx'];
    var maxSize = 5 * 1024 * 1024;
    var err = document.getElementById('fileError');
    if (!err) return;
    err.style.display = 'none';
    err.innerText = '';
    if (existingCount + input.files.length > 5) {
        var canAdd = 5 - existingCount;
        err.innerText = 'You already have ' + existingCount + ' attachment(s). You can only add ' + canAdd + ' more (max 5 total).';
        err.style.display = 'block';
        input.value = '';
        return;
    }
    for (var i = 0; i < input.files.length; i++) {
        var f = input.files[i];
        var ext = f.name.substring(f.name.lastIndexOf('.')).toLowerCase();
        if (allowed.indexOf(ext) === -1) {
            err.innerText = "'" + f.name + "' has an invalid file type. Allowed: jpg, jpeg, png, gif, bmp, pdf, txt, doc, docx.";
            err.style.display = 'block';
            input.value = '';
            return;
        }
        if (f.size > maxSize) {
            err.innerText = "'" + f.name + "' exceeds the 5 MB size limit.";
            err.style.display = 'block';
            input.value = '';
            return;
        }
    }
}

window.validateFiles = validateFiles;
