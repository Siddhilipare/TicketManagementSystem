(function () {
    var allowedExtensions = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'pdf', 'txt', 'doc', 'docx'];
    var maxSizeBytes = 5 * 1024 * 1024;
    var maxFiles = 5;

    var input = document.getElementById('attachmentInput');
    if (!input) return;

    var chipList = document.getElementById('fileChipList');
    var errorDiv = document.getElementById('fileError');
    var dt = new DataTransfer();

    function iconForExt(ext) {
        if (ext === 'pdf') return 'fa-file-pdf';
        if (ext === 'doc' || ext === 'docx') return 'fa-file-word';
        if (ext === 'txt') return 'fa-file-lines';
        return 'fa-file';
    }

    function formatSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return Math.round(bytes / 1024) + ' KB';
        return (bytes / 1024 / 1024).toFixed(1) + ' MB';
    }

    function isImageExt(ext) {
        return ['jpg', 'jpeg', 'png', 'gif', 'bmp'].indexOf(ext) !== -1;
    }

    function render() {
        if (!chipList) return;
        chipList.innerHTML = '';
        Array.from(dt.files).forEach(function (file, idx) {
            var ext = file.name.substring(file.name.lastIndexOf('.') + 1).toLowerCase();
            var chip = document.createElement('div');
            chip.className = 'file-chip';

            var iconHtml;
            if (isImageExt(ext)) {
                var url = URL.createObjectURL(file);
                iconHtml = '<img src="' + url + '" class="file-chip-thumb" />';
            } else {
                iconHtml = '<div class="file-chip-icon"><i class="fa-solid ' + iconForExt(ext) + '"></i></div>';
            }

            chip.innerHTML = iconHtml +
                '<div class="file-chip-info"><div class="file-chip-name">' + file.name + '</div>' +
                '<div class="file-chip-size">' + formatSize(file.size) + '</div></div>' +
                '<button type="button" class="file-chip-remove" data-idx="' + idx + '">&times;</button>';

            chipList.appendChild(chip);
        });

        Array.prototype.forEach.call(chipList.querySelectorAll('.file-chip-remove'), function (btn) {
            btn.addEventListener('click', function () {
                removeFile(parseInt(btn.getAttribute('data-idx'), 10));
            });
        });

        input.files = dt.files;
    }

    function removeFile(idx) {
        var newDt = new DataTransfer();
        Array.from(dt.files).forEach(function (f, i) {
            if (i !== idx) newDt.items.add(f);
        });
        dt = newDt;
        render();
    }

    input.addEventListener('change', function () {
        if (errorDiv) errorDiv.style.display = 'none';
        var selected = Array.from(input.files);

        for (var i = 0; i < selected.length; i++) {
            var file = selected[i];
            var ext = file.name.substring(file.name.lastIndexOf('.') + 1).toLowerCase();

            if (dt.files.length >= maxFiles) {
                if (errorDiv) {
                    errorDiv.textContent = 'You can attach a maximum of ' + maxFiles + ' files.';
                    errorDiv.style.display = 'block';
                }
                break;
            }
            if (allowedExtensions.indexOf(ext) === -1) {
                if (errorDiv) {
                    errorDiv.textContent = 'File type not allowed: "' + file.name + '". Allowed: PNG, JPG, GIF, BMP, PDF, TXT, DOC, DOCX.';
                    errorDiv.style.display = 'block';
                }
                continue;
            }
            if (file.size > maxSizeBytes) {
                if (errorDiv) {
                    errorDiv.textContent = 'File "' + file.name + '" exceeds the 5MB size limit.';
                    errorDiv.style.display = 'block';
                }
                continue;
            }
            dt.items.add(file);
        }
        render();
    });
})();
