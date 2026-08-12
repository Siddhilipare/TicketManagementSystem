var CA_PAGE_SIZE   = 6;
var CA_STORAGE_KEY = 'supportCA_state';
var caCurrentPage  = 1;

var caFilterFormEl = document.getElementById('caFilterForm');
var searchVal   = caFilterFormEl ? (caFilterFormEl.dataset.search || '') : '';
var dateVal     = caFilterFormEl ? (caFilterFormEl.dataset.date || '') : '';
var sortVal     = caFilterFormEl ? (caFilterFormEl.dataset.sortOrder || 'newest') : 'newest';
var priorityVal = caFilterFormEl ? (caFilterFormEl.dataset.priorityFilter || '') : '';

var caCurrentState = {
    page:           1,
    search:         searchVal,
    date:           dateVal,
    sortOrder:      sortVal,
    priorityFilter: priorityVal
};

function saveCAState() {
    caCurrentState.page = caCurrentPage;
    sessionStorage.setItem(CA_STORAGE_KEY, JSON.stringify(caCurrentState));
}

function clearCAState() {
    sessionStorage.removeItem(CA_STORAGE_KEY);
}

function loadAndRedirectCA() {
    var saved = sessionStorage.getItem(CA_STORAGE_KEY);
    if (!saved) return false;

    var state = JSON.parse(saved);
    var filtersMatch =
        state.search         === caCurrentState.search &&
        state.date           === caCurrentState.date &&
        state.sortOrder      === caCurrentState.sortOrder &&
        state.priorityFilter === caCurrentState.priorityFilter;

    if (filtersMatch) {
        caCurrentPage = parseInt(state.page) || 1;
        return false;
    }

    var url = '/Support/CompletedArchive?';
    if (state.search)         url += 'search='         + encodeURIComponent(state.search)         + '&';
    if (state.date)           url += 'date='           + encodeURIComponent(state.date)           + '&';
    if (state.sortOrder)      url += 'sortOrder='      + encodeURIComponent(state.sortOrder)      + '&';
    if (state.priorityFilter) url += 'priorityFilter=' + encodeURIComponent(state.priorityFilter) + '&';

    sessionStorage.setItem(CA_STORAGE_KEY, JSON.stringify(state));
    window.location.href = url;
    return true;
}

function caRenderPage(page) {
    var cards      = document.querySelectorAll('.ca-card');
    var total      = cards.length;
    var totalPages = Math.ceil(total / CA_PAGE_SIZE);
    if (page < 1) page = 1;
    if (page > totalPages) page = totalPages || 1;
    caCurrentPage = page;

    var start = (page - 1) * CA_PAGE_SIZE;
    var end   = start + CA_PAGE_SIZE;

    cards.forEach(function (card, i) {
        card.style.display = (i >= start && i < end) ? '' : 'none';
    });

    caRenderPagination(page, totalPages, total);
}

function caRenderPagination(page, totalPages, total) {
    var bar  = document.getElementById('caPaginationBar');
    var info = document.getElementById('caPageInfo');
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
    prev.onclick   = function () { caRenderPage(page - 1); saveCAState(); };
    bar.appendChild(prev);

    var startPage = Math.max(1, page - 2);
    var endPage   = Math.min(totalPages, page + 2);

    if (startPage > 1) { caAppendBtn(bar, 1, page); if (startPage > 2) bar.appendChild(caDots()); }
    for (var i = startPage; i <= endPage; i++) caAppendBtn(bar, i, page);
    if (endPage < totalPages) { if (endPage < totalPages - 1) bar.appendChild(caDots()); caAppendBtn(bar, totalPages, page); }

    var next = document.createElement('button');
    next.className = page === totalPages ? 'btn-secondary-gold' : 'btn-primary-gold';
    next.style.cssText = 'padding:6px 14px;font-size:13px;';
    next.innerHTML = '<i class="fa-solid fa-chevron-right"></i>';
    next.disabled  = page === totalPages;
    next.onclick   = function () { caRenderPage(page + 1); saveCAState(); };
    bar.appendChild(next);

    var s = (page - 1) * CA_PAGE_SIZE + 1;
    var e = Math.min(page * CA_PAGE_SIZE, total);
    if (info) info.textContent = 'Showing ' + s + ' \u2013 ' + e + ' of ' + total + ' tickets';
}

function caAppendBtn(bar, num, activePage) {
    var btn = document.createElement('button');
    btn.textContent   = num;
    btn.style.cssText = 'padding:6px 12px;font-size:13px;min-width:36px;' + (num === activePage ? 'font-weight:700;' : '');
    btn.className     = num === activePage ? 'btn-primary-gold' : 'btn-secondary-gold';
    btn.onclick       = (function (n) { return function () { caRenderPage(n); saveCAState(); }; })(num);
    bar.appendChild(btn);
}

function caDots() {
    var s = document.createElement('span');
    s.textContent   = '...';
    s.style.cssText = 'color:var(--text-tertiary);padding:0 4px;font-size:13px;';
    return s;
}

window.saveCAState = saveCAState;
window.clearCAState = clearCAState;
window.caRenderPage = caRenderPage;

document.addEventListener('DOMContentLoaded', function () {
    var cards = document.querySelectorAll('.ca-card');
    if (cards.length === 0) return;

    var redirecting = loadAndRedirectCA();
    if (!redirecting) caRenderPage(caCurrentPage);
});
