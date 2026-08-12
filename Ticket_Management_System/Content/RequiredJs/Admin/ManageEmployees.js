var PAGE_SIZE = 6;
var STORAGE_KEY = 'adminEmployees_state';
var currentPage = 1;

var filterFormEl = document.getElementById('employeeFilterForm');
var initialSearch = filterFormEl ? (filterFormEl.dataset.search || '') : '';

var currentState = {
    page: 1,
    search: initialSearch
};

function saveState() {
    currentState.page = currentPage;
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(currentState));
}

function clearState() {
    sessionStorage.removeItem(STORAGE_KEY);
}

function loadAndRedirectIfNeeded() {
    var saved = sessionStorage.getItem(STORAGE_KEY);
    if (!saved) return false;

    var state = JSON.parse(saved);

    var filtersMatch = state.search === currentState.search;

    if (filtersMatch) {
        currentPage = parseInt(state.page) || 1;
        return false;
    }

    var url = '/Admin/ManageEmployees';
    if (state.search) url += '?search=' + encodeURIComponent(state.search);

    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    window.location.href = url;
    return true;
}

function renderPage(page) {
    var rows = document.querySelectorAll('.employee-row');
    var total = rows.length;
    var totalPages = Math.ceil(total / PAGE_SIZE);
    if (page < 1) page = 1;
    if (page > totalPages) page = totalPages || 1;
    currentPage = page;

    var start = (page - 1) * PAGE_SIZE;
    var end = start + PAGE_SIZE;

    rows.forEach(function (row, i) {
        row.style.display = (i >= start && i < end) ? '' : 'none';
    });

    renderPagination(page, totalPages, total);
}

function renderPagination(page, totalPages, total) {
    var bar  = document.getElementById('paginationBar');
    var info = document.getElementById('pageInfo');
    if (!bar) return;
    bar.innerHTML = '';

    if (totalPages <= 1) {
        if (info) info.textContent = total > 0 ? 'Showing all ' + total + ' employees' : '';
        return;
    }

    var prev = document.createElement('button');
    prev.className = page === 1 ? 'btn-secondary-gold' : 'btn-primary-gold';
    prev.style.cssText = 'padding: 6px 14px; font-size: 13px;';
    prev.innerHTML = '<i class="fa-solid fa-chevron-left"></i>';
    prev.disabled = page === 1;
    prev.onclick = function () { renderPage(page - 1); saveState(); };
    bar.appendChild(prev);

    var startPage = Math.max(1, page - 2);
    var endPage   = Math.min(totalPages, page + 2);

    if (startPage > 1) {
        appendPageBtn(bar, 1, page);
        if (startPage > 2) { bar.appendChild(makeDots()); }
    }

    for (var i = startPage; i <= endPage; i++) { appendPageBtn(bar, i, page); }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) { bar.appendChild(makeDots()); }
        appendPageBtn(bar, totalPages, page);
    }

    var next = document.createElement('button');
    next.className = page === totalPages ? 'btn-secondary-gold' : 'btn-primary-gold';
    next.style.cssText = 'padding: 6px 14px; font-size: 13px;';
    next.innerHTML = '<i class="fa-solid fa-chevron-right"></i>';
    next.disabled = page === totalPages;
    next.onclick = function () { renderPage(page + 1); saveState(); };
    bar.appendChild(next);

    var s = (page - 1) * PAGE_SIZE + 1;
    var e = Math.min(page * PAGE_SIZE, total);
    if (info) info.textContent = 'Showing ' + s + ' \u2013 ' + e + ' of ' + total + ' employees';
}

function appendPageBtn(bar, num, activePage) {
    var btn = document.createElement('button');
    btn.textContent = num;
    btn.style.cssText = 'padding: 6px 12px; font-size: 13px; min-width: 36px;' + (num === activePage ? 'font-weight: 700;' : '');
    btn.className = num === activePage ? 'btn-primary-gold' : 'btn-secondary-gold';
    btn.onclick = (function (n) { return function () { renderPage(n); saveState(); }; })(num);
    bar.appendChild(btn);
}

function makeDots() {
    var s = document.createElement('span');
    s.textContent = '...';
    s.style.cssText = 'color: var(--text-tertiary); padding: 0 4px; font-size: 13px;';
    return s;
}

window.saveState = saveState;
window.clearState = clearState;
window.renderPage = renderPage;

document.addEventListener('DOMContentLoaded', function () {
    var rows = document.querySelectorAll('.employee-row');
    if (rows.length === 0) return;

    var redirecting = loadAndRedirectIfNeeded();
    if (!redirecting) {
        renderPage(currentPage);
    }
});
