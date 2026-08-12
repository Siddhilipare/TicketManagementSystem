var NP_PAGE_SIZE   = 6;
var NP_STORAGE_KEY = 'supportNP_state';
var npCurrentPage  = 1;

var npFilterFormEl = document.getElementById('npFilterForm');
var searchVal = npFilterFormEl ? (npFilterFormEl.dataset.search || '') : '';
var dateVal   = npFilterFormEl ? (npFilterFormEl.dataset.date || '') : '';
var sortVal   = npFilterFormEl ? (npFilterFormEl.dataset.sortOrder || 'newest') : 'newest';

var npCurrentState = {
    page:      1,
    search:    searchVal,
    date:      dateVal,
    sortOrder: sortVal
};

function saveNPState() {
    npCurrentState.page = npCurrentPage;
    sessionStorage.setItem(NP_STORAGE_KEY, JSON.stringify(npCurrentState));
}

function clearNPState() {
    sessionStorage.removeItem(NP_STORAGE_KEY);
}

function loadAndRedirectNP() {
    var saved = sessionStorage.getItem(NP_STORAGE_KEY);
    if (!saved) return false;

    var state = JSON.parse(saved);
    var filtersMatch =
        state.search    === npCurrentState.search &&
        state.date      === npCurrentState.date &&
        state.sortOrder === npCurrentState.sortOrder;

    if (filtersMatch) {
        npCurrentPage = parseInt(state.page) || 1;
        return false;
    }

    var url = '/Support/NeedsPriority?';
    if (state.search)    url += 'search='    + encodeURIComponent(state.search)    + '&';
    if (state.date)      url += 'date='      + encodeURIComponent(state.date)      + '&';
    if (state.sortOrder) url += 'sortOrder=' + encodeURIComponent(state.sortOrder) + '&';

    sessionStorage.setItem(NP_STORAGE_KEY, JSON.stringify(state));
    window.location.href = url;
    return true;
}

function npRenderPage(page) {
    var cards      = document.querySelectorAll('.np-card');
    var total      = cards.length;
    var totalPages = Math.ceil(total / NP_PAGE_SIZE);
    if (page < 1) page = 1;
    if (page > totalPages) page = totalPages || 1;
    npCurrentPage = page;

    var start = (page - 1) * NP_PAGE_SIZE;
    var end   = start + NP_PAGE_SIZE;

    cards.forEach(function (card, i) {
        card.style.display = (i >= start && i < end) ? '' : 'none';
    });

    npRenderPagination(page, totalPages, total);
}

function npRenderPagination(page, totalPages, total) {
    var bar  = document.getElementById('npPaginationBar');
    var info = document.getElementById('npPageInfo');
    if (!bar) return;
    bar.innerHTML = '';

    if (totalPages <= 1) {
        if (info) info.textContent = total > 0 ? 'Showing all ' + total + ' tickets' : '';
        return;
    }

    var prev = document.createElement('button');
    prev.className = page === 1 ? 'btn-secondary-gold' : 'btn-primary-gold';
    prev.style.cssText = 'padding:6px 14px;font-size:13px;';
    prev.innerHTML = '<i class="fa-solid fa-chevron-left"></i>';
    prev.disabled  = page === 1;
    prev.onclick   = function () { npRenderPage(page - 1); saveNPState(); };
    bar.appendChild(prev);

    var startPage = Math.max(1, page - 2);
    var endPage   = Math.min(totalPages, page + 2);

    if (startPage > 1) { npAppendBtn(bar, 1, page); if (startPage > 2) bar.appendChild(npDots()); }
    for (var i = startPage; i <= endPage; i++) npAppendBtn(bar, i, page);
    if (endPage < totalPages) { if (endPage < totalPages - 1) bar.appendChild(npDots()); npAppendBtn(bar, totalPages, page); }

    var next = document.createElement('button');
    next.className = page === totalPages ? 'btn-secondary-gold' : 'btn-primary-gold';
    next.style.cssText = 'padding:6px 14px;font-size:13px;';
    next.innerHTML = '<i class="fa-solid fa-chevron-right"></i>';
    next.disabled  = page === totalPages;
    next.onclick   = function () { npRenderPage(page + 1); saveNPState(); };
    bar.appendChild(next);

    var s = (page - 1) * NP_PAGE_SIZE + 1;
    var e = Math.min(page * NP_PAGE_SIZE, total);
    if (info) info.textContent = 'Showing ' + s + ' \u2013 ' + e + ' of ' + total + ' tickets';
}

function npAppendBtn(bar, num, activePage) {
    var btn = document.createElement('button');
    btn.textContent   = num;
    btn.style.cssText = 'padding:6px 12px;font-size:13px;min-width:36px;' + (num === activePage ? 'font-weight:700;' : '');
    btn.className     = num === activePage ? 'btn-primary-gold' : 'btn-secondary-gold';
    btn.onclick       = (function (n) { return function () { npRenderPage(n); saveNPState(); }; })(num);
    bar.appendChild(btn);
}

function npDots() {
    var s = document.createElement('span');
    s.textContent   = '...';
    s.style.cssText = 'color:var(--text-tertiary);padding:0 4px;font-size:13px;';
    return s;
}

window.saveNPState = saveNPState;
window.clearNPState = clearNPState;
window.npRenderPage = npRenderPage;

document.addEventListener('DOMContentLoaded', function () {
    var cards = document.querySelectorAll('.np-card');
    if (cards.length === 0) return;
    var redirecting = loadAndRedirectNP();
    if (!redirecting) npRenderPage(npCurrentPage);
});
