/**
 * Ticket Management System - Premium Luxury Client UI Module
 * Handles Dual-Theme Manager, Page Loading Overlay, Consolidated Toast Notifications,
 * and Rolls-Royce Sidebar Collapse/Expand Controller.
 */

(function () {
    'use strict';

    // =========================================================================
    // 1. DUAL THEME SYSTEM MANAGER
    // =========================================================================
    function getStoredTheme() {
        return localStorage.getItem('theme-preference');
    }

    function getSystemTheme() {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme-preference', theme);
    }

    function initTheme() {
        var savedTheme = getStoredTheme();
        var theme = savedTheme || getSystemTheme() || 'dark';
        applyTheme(theme);
    }

    function toggleTheme() {
        var currentTheme = document.documentElement.getAttribute('data-theme') || 'dark';
        var newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        
        var btn = document.getElementById('theme-toggle');
        if (btn) {
            btn.style.transition = 'transform 300ms ease-in-out';
            btn.style.transform = 'rotate(180deg)';
            setTimeout(function () {
                btn.style.transform = 'rotate(0deg)';
            }, 300);
        }

        applyTheme(newTheme);
    }

    // =========================================================================
    // 2. CONSOLIDATED TOAST NOTIFICATION SYSTEM (3s Duration, Consolidation)
    // =========================================================================
    var ToastManager = {
        queue: [],
        timer: null,
        lastToastTime: 0,
        toastCount: 0,

        initContainer: function () {
            var container = document.getElementById('toastContainer');
            if (!container) {
                container = document.createElement('div');
                container.id = 'toastContainer';
                container.className = 'toast-container';
                document.body.appendChild(container);
            }
            return container;
        },

        show: function (type, title, message) {
            var container = this.initContainer();
            var now = Date.now();

            // Consolidation logic: If multiple toasts arrive within 2000ms
            if (now - this.lastToastTime < 2000 && container.children.length > 0) {
                var existingToast = container.lastElementChild;
                if (existingToast && existingToast.classList.contains('toast-' + type)) {
                    this.toastCount++;
                    var msgElement = existingToast.querySelector('.toast-message');
                    if (msgElement) {
                        msgElement.innerText = this.toastCount + ' operations completed successfully.';
                    }
                    this.lastToastTime = now;
                    return;
                }
            }

            this.toastCount = 1;
            this.lastToastTime = now;

            var toast = document.createElement('div');
            toast.className = 'toast-item toast-' + type;

            var iconClass = 'fa-circle-check';
            if (type === 'error') iconClass = 'fa-circle-xmark';
            else if (type === 'warning') iconClass = 'fa-triangle-exclamation';
            else if (type === 'info') iconClass = 'fa-circle-info';

            toast.innerHTML = 
                '<i class="fa-solid ' + iconClass + ' toast-icon"></i>' +
                '<div class="toast-content">' +
                    '<div class="toast-title">' + (title || 'Notification') + '</div>' +
                    '<div class="toast-message">' + (message || '') + '</div>' +
                '</div>' +
                '<button type="button" class="toast-close" aria-label="Close">&times;</button>';

            container.appendChild(toast);

            // Animate in
            setTimeout(function () {
                toast.classList.add('show');
            }, 50);

            // Auto dismiss after 3 seconds (3000ms)
            var dismissTimer = setTimeout(function () {
                ToastManager.remove(toast);
            }, 3000);

            // Close button click
            var closeBtn = toast.querySelector('.toast-close');
            if (closeBtn) {
                closeBtn.addEventListener('click', function () {
                    clearTimeout(dismissTimer);
                    ToastManager.remove(toast);
                });
            }
        },

        remove: function (toast) {
            toast.classList.remove('show');
            setTimeout(function () {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }
    };

    // =========================================================================
    // 3. PAGE NAVIGATION & ACTION LOADING OVERLAY (2-SECOND FEEDBACK)
    // =========================================================================
    var LoadingManager = {
        overlay: null,

        initOverlay: function () {
            if (!this.overlay) {
                this.overlay = document.getElementById('pageLoadingOverlay');
                if (!this.overlay) {
                    this.overlay = document.createElement('div');
                    this.overlay.id = 'pageLoadingOverlay';
                    this.overlay.className = 'loading-overlay';
                    this.overlay.innerHTML = 
                        '<div class="spinner-ring"></div>' +
                        '<div class="loading-text">Loading...</div>';
                    document.body.appendChild(this.overlay);
                }
            }
            return this.overlay;
        },

        show: function (msg, duration, callback) {
            var overlay = this.initOverlay();
            var textEl = overlay.querySelector('.loading-text');
            if (textEl && msg) {
                textEl.innerText = msg;
            }
            overlay.classList.add('active');

            var minDisplayTime = duration || 2000; // 2 seconds minimum
            setTimeout(function () {
                if (typeof callback === 'function') {
                    callback();
                }
            }, minDisplayTime);
        },

        hide: function () {
            if (this.overlay) {
                this.overlay.classList.remove('active');
            }
        }
    };

    // Intercept clicks on links for smooth page transition loader
    function setupLinkTransitions() {
        document.addEventListener('click', function (e) {
            var link = e.target.closest('a');
            if (link && link.href && !link.target && !link.hasAttribute('data-toggle') && link.getAttribute('href') !== '#') {
                var url = link.href;
                if (url.startsWith(window.location.origin) || url.startsWith('/')) {
                    e.preventDefault();
                    LoadingManager.show('Loading page...', 2000, function () {
                        window.location.href = url;
                    });
                }
            }
        });

        // Intercept form submissions
        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (!form.hasAttribute('data-no-loader')) {
                LoadingManager.show('Processing action...', 2000);
            }
        });
    }

    // =========================================================================
    // 4. VERTICAL SIDEBAR COLLAPSE / EXPAND CONTROLLER
    // =========================================================================
    function initSidebar() {
        var toggleBtn = document.getElementById('sidebarToggleBtn');
        var sidebar = document.getElementById('appSidebar');
        var backdrop = document.getElementById('sidebarBackdrop');
        if (!sidebar) return;

        function openMobileSidebar() {
            sidebar.classList.add('mobile-open');
            if (backdrop) backdrop.classList.add('active');
            document.body.style.overflow = 'hidden';
        }

        function closeMobileSidebar() {
            sidebar.classList.remove('mobile-open');
            if (backdrop) backdrop.classList.remove('active');
            document.body.style.overflow = '';
        }

        var savedState = localStorage.getItem('sidebar-state');
        if (savedState === 'collapsed') {
            sidebar.classList.add('collapsed');
        } else if (savedState === 'expanded') {
            sidebar.classList.remove('collapsed');
        }

        if (toggleBtn) {
            toggleBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                if (window.innerWidth <= 991) {
                    if (sidebar.classList.contains('mobile-open')) {
                        closeMobileSidebar();
                    } else {
                        openMobileSidebar();
                    }
                } else {
                    sidebar.classList.toggle('collapsed');
                    var isCollapsed = sidebar.classList.contains('collapsed');
                    localStorage.setItem('sidebar-state', isCollapsed ? 'collapsed' : 'expanded');
                }
            });
        }

        if (backdrop) {
            backdrop.addEventListener('click', closeMobileSidebar);
        }

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && sidebar.classList.contains('mobile-open')) {
                closeMobileSidebar();
            }
        });

        sidebar.addEventListener('click', function (e) {
            if (window.innerWidth <= 991 && e.target.closest('a')) {
                closeMobileSidebar();
            }
        });

        window.addEventListener('resize', function () {
            if (window.innerWidth > 991 && sidebar.classList.contains('mobile-open')) {
                closeMobileSidebar();
            }
        });
    }

    // Initialize on DOMReady
    document.addEventListener('DOMContentLoaded', function () {
        initSidebar();
        setupLinkTransitions();

        var themeBtn = document.getElementById('theme-toggle');
        if (themeBtn) {
            themeBtn.addEventListener('click', toggleTheme);
        }
    });

    // Expose global helpers
    window.LuxuryUI = {
        toggleTheme: toggleTheme,
        showToast: function (type, title, message) {
            ToastManager.show(type, title, message);
        },
        showLoader: function (msg, duration, callback) {
            LoadingManager.show(msg, duration, callback);
        },
        hideLoader: function () {
            LoadingManager.hide();
        }
    };

})();
