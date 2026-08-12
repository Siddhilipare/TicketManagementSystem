(function (window, document) {
    'use strict';

    // =========================================================================
    // FORM-HELPER BOT (Login / Register / ManageUsers pages)
    // =========================================================================

    function createBotDOM() {
        if (document.getElementById('simplifyAssistantBot')) return;

        var wrapper = document.createElement('div');
        wrapper.id = 'simplifyAssistantBot';
        wrapper.className = 'bot-assistant-wrapper bot-state-idle';
        wrapper.setAttribute('aria-hidden', 'true');

        wrapper.innerHTML =
            '<div class="bot-speech-bubble bot-tooltip hidden" id="simplifyBotTooltip">Hi there! I\'m your assistant bot.</div>' +
            '<div class="bot-character">' +
                '<div class="bot-antenna"><div class="bot-antenna-ball"></div></div>' +
                '<div class="bot-head">' +
                    '<div class="bot-face">' +
                        '<div class="bot-eye bot-eye-left"><div class="bot-pupil"></div></div>' +
                        '<div class="bot-eye bot-eye-right"><div class="bot-pupil"></div></div>' +
                        '<div class="bot-mouth"></div>' +
                    '</div>' +
                    '<div class="bot-hand bot-hand-left"></div>' +
                    '<div class="bot-hand bot-hand-right"></div>' +
                '</div>' +
                '<div class="bot-body-badge">S</div>' +
            '</div>';

        document.body.appendChild(wrapper);
    }

    function glideTo(inputEl) {
        var wrapper = document.querySelector('.bot-assistant-wrapper');
        if (!wrapper || !inputEl) return;

        var rect = inputEl.getBoundingClientRect();
        var scrollTop = window.pageYOffset || document.documentElement.scrollTop;

        var botWidth = wrapper.offsetWidth || 72;
        var botHeight = wrapper.offsetHeight || 80;
        var targetTop = rect.top + scrollTop + (rect.height / 2) - (botHeight / 2);
        var targetLeft = rect.right + 16;

        if (targetLeft + botWidth > window.innerWidth - 24) {
            targetLeft = rect.left - botWidth - 16;
        }

        wrapper.style.top = targetTop + 'px';
        wrapper.style.left = targetLeft + 'px';
        wrapper.style.opacity = '1';
        wrapper.style.transform = 'scale(1)';
    }

    function wireFieldListeners(formSelector) {
        var form = document.querySelector(formSelector);
        if (!form) return;

        var inputs = form.querySelectorAll('input, select, textarea');
        inputs.forEach(function (input) {
            input.addEventListener('focus', function () { glideTo(this); evaluateField(this); });
            input.addEventListener('input', function () { evaluateField(this); });
            if (input.type === 'password') {
                input.addEventListener('focus', coverEyes);
                input.addEventListener('blur', uncoverEyes);
                input.addEventListener('input', function () { checkPasswordStrength(input); });
            }
        });

        var firstInput = form.querySelector('input:not([type="hidden"])');
        if (firstInput) { setTimeout(function () { glideTo(firstInput); }, 200); }
    }

    function evaluateField(input) {
        var name = (input.name || input.id || '').toLowerCase();
        var val = input.value || '';

        if (name.indexOf('username') !== -1 || (name.indexOf('name') !== -1 && name.indexOf('email') === -1)) {
            validateName(val, input);
        } else if (name === 'age') {
            validateAge(val, input);
        } else if (input.type === 'email' || name.indexOf('email') !== -1) {
            validateEmail(val, input);
        } else if (name === 'confirmpassword' || name.indexOf('confirm') !== -1) {
            validateConfirm(val, input);
        }
    }

    function validateName(val, input) {
        if (val.length === 0) { clearBubble(input); return; }
        if (/[^a-zA-Z\s]/.test(val)) {
            botShake(); showBubble("Names can only contain letters.", 'error');
            setBotEyeError(true); if (input) input.classList.add('input-invalid');
        } else {
            botBounce(); clearBubble(input); setBotEyeError(false);
            if (input) { input.classList.remove('input-invalid'); input.classList.add('input-valid'); }
        }
    }

    function validateAge(val, input) {
        if (val.length === 0) { clearBubble(input); return; }
        if (/\D/.test(val)) { botShake(); showBubble("Age must be a number.", 'error'); if (input) input.classList.add('input-invalid'); return; }
        var age = parseInt(val, 10);
        if (age < 16) { botShake(); showBubble("You must be at least 16.", 'error'); if (input) input.classList.add('input-invalid'); return; }
        if (age > 100) { botShake(); showBubble("Please enter a realistic age.", 'error'); if (input) input.classList.add('input-invalid'); return; }
        botBounce(); clearBubble(input);
        if (input) { input.classList.remove('input-invalid'); input.classList.add('input-valid'); }
    }

    function validateEmail(val, input) {
        if (val.length < 4) { clearBubble(input); return; }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val)) {
            botShake(); showBubble("That doesn't look like a valid email.", 'error');
            if (input) input.classList.add('input-invalid');
        } else {
            botBounce(); clearBubble(input);
            if (input) { input.classList.remove('input-invalid'); input.classList.add('input-valid'); }
        }
    }

    function validateConfirm(val, input) {
        var pwField = document.querySelector('input[name="Password"], input[type="password"]:not([name="ConfirmPassword"])');
        if (!pwField) return;
        if (val.length === 0) { clearBubble(input); return; }
        if (val !== pwField.value) {
            botShake(); showBubble("Passwords don't match yet.", 'error');
            if (input) input.classList.add('input-invalid');
        } else if (val.length >= 8) {
            botBounce(); showBubble("Passwords match! \u2713", 'success');
            if (input) { input.classList.remove('input-invalid'); input.classList.add('input-valid'); }
        }
    }

    function checkPasswordStrength(input) {
        var val = input.value;
        if (val.length < 8) {
            showBubble("Need at least 8 characters.", 'error'); peekOneEye();
            if (input) input.classList.add('input-invalid');
        } else {
            showBubble("Strong enough!", 'success'); coverEyes(); botBounce();
            if (input) { input.classList.remove('input-invalid'); input.classList.add('input-valid'); }
        }
    }

    var _onPasswordField = false;

    function coverEyes() {
        _onPasswordField = true;
        var char = document.querySelector('.bot-character');
        if (!char) return;
        char.classList.remove('bot-state-peek');
        char.classList.add('bot-state-covering');
    }

    function uncoverEyes() {
        _onPasswordField = false;
        var char = document.querySelector('.bot-character');
        if (!char) return;
        char.classList.remove('bot-state-covering', 'bot-state-peek');
    }

    function peekOneEye() {
        var char = document.querySelector('.bot-character');
        if (!char) return;
        char.classList.remove('bot-state-covering');
        char.classList.add('bot-state-peek');
    }

    function setBotEyeError(isError) {
        var head = document.querySelector('.bot-character');
        if (!head) return;
        if (isError) { head.classList.add('bot-eye-error'); } else { head.classList.remove('bot-eye-error'); }
    }

    function botShake() {
        var char = document.querySelector('.bot-character');
        if (!char) return;
        char.classList.remove('bot-state-shake'); void char.offsetWidth; char.classList.add('bot-state-shake');
    }

    function botBounce() {
        var char = document.querySelector('.bot-character');
        if (!char) return;
        char.classList.remove('bot-state-happy'); void char.offsetWidth; char.classList.add('bot-state-happy');
    }

    function showBubble(text, type) {
        var bubble = document.querySelector('.bot-speech-bubble');
        if (!bubble) return;
        bubble.textContent = text;
        bubble.classList.remove('hidden', 'active'); void bubble.offsetWidth; bubble.classList.add('active');
        bubble.style.borderColor = type === 'error' ? 'var(--error)' : 'var(--success)';
        bubble.style.color = type === 'error' ? 'var(--error)' : 'var(--success)';
    }

    function clearBubble(input) {
        var bubble = document.querySelector('.bot-speech-bubble');
        if (bubble) bubble.classList.add('hidden');
        if (input) input.classList.remove('input-invalid');
    }

    function wireSubmitSpin(formSelector) {
        var form = document.querySelector(formSelector);
        if (!form) return;
        var btn = form.querySelector('[type="submit"]');
        if (!btn) return;

        btn.addEventListener('click', function (e) {
            e.preventDefault();

            // The spin animation below finishes by calling form.submit()
            // directly, which does NOT fire the form's 'submit' event — so
            // the char-limit guard has to be checked here too, or an
            // over-limit field could sail through while the bot is spinning.
            if (window.AssistantBot && window.AssistantBot.CharLimitGuard &&
                !window.AssistantBot.CharLimitGuard.validateForm(form)) {
                return;
            }

            var wrapper = document.querySelector('.bot-assistant-wrapper');
            var rect = btn.getBoundingClientRect();
            var scrollTop = window.pageYOffset || 0;
            if (wrapper) {
                wrapper.style.top = (rect.top + scrollTop - 40) + 'px';
                wrapper.style.left = (rect.left + rect.width / 2 - 36) + 'px';
            }
            setTimeout(function () {
                if (wrapper) { wrapper.style.transition = 'none'; wrapper.style.animation = 'botSpin 700ms ease-in-out 3'; }
                setTimeout(function () {
                    if (wrapper) { wrapper.style.opacity = '0'; wrapper.style.transform = 'scale(0)'; }
                    setTimeout(function () { form.submit(); }, 350);
                }, 2100);
            }, 400);
        });
    }

    // =========================================================================
    // CHARACTER LIMIT GUARD
    // Live character counters + max-length validation, reused across every
    // form the bot watches (Employee Registration, Ticket Creation,
    // Complaint Submission, Admin User Management).
    //
    // Opt a field in with either:
    //   - native maxlength="100"                     (simple fields)
    //   - data-max-length="100" data-min-length="3"   (fields the bot should
    //     fully own: value is NOT hard-truncated by the browser, so typing
    //     or pasting past the limit is possible and triggers the live
    //     warning below, exactly like it would if HTML markup validation
    //     rejected the request server-side)
    //   - data-field-label="Ticket Description"        (optional, used in
    //     the message; falls back to the field's <label>, name, or id)
    // =========================================================================

    var CharLimitGuard = (function () {

        var GUARD_SELECTOR =
            'input[data-max-length], textarea[data-max-length], ' +
            'input[maxlength], textarea[maxlength]';

        function isTextLike(el) {
            if (!el || !el.tagName) return false;
            if (el.tagName === 'TEXTAREA') return true;
            if (el.tagName !== 'INPUT') return false;
            var t = (el.type || 'text').toLowerCase();
            return t === 'text' || t === 'email' || t === 'password' || t === 'search' || t === 'tel' || t === 'url';
        }

        function getMax(el) {
            var v = el.getAttribute('data-max-length') || el.getAttribute('maxlength');
            var n = parseInt(v, 10);
            return isNaN(n) || n <= 0 ? null : n;
        }

        function getMin(el) {
            var v = el.getAttribute('data-min-length') || el.getAttribute('minlength');
            var n = parseInt(v, 10);
            return isNaN(n) || n <= 0 ? 0 : n;
        }

        function getLabel(el) {
            var explicit = el.getAttribute('data-field-label');
            if (explicit) return explicit;
            if (el.id) {
                var lab = document.querySelector('label[for="' + el.id + '"]');
                if (lab) return lab.textContent.replace(/\*/g, '').trim();
            }
            var group = el.closest ? el.closest('.form-group') : null;
            if (group) {
                var l = group.querySelector('label');
                if (l) return l.textContent.replace(/\*/g, '').trim();
            }
            return el.name || el.id || 'This field';
        }

        function fieldKey(el) {
            if (!el.__charLimitKey) {
                el.__charLimitKey = 'cl_' + (el.id || el.name || Math.random().toString(36).slice(2));
            }
            return el.__charLimitKey;
        }

        function ensureCounter(el) {
            var id = fieldKey(el) + '_counter';
            var counter = document.getElementById(id);
            if (!counter) {
                counter = document.createElement('div');
                counter.id = id;
                counter.className = 'char-limit-counter';
                if (el.parentNode) el.parentNode.insertBefore(counter, el.nextSibling);
            }
            return counter;
        }

        function ensureError(el, counter) {
            var id = fieldKey(el) + '_charError';
            var err = document.getElementById(id);
            if (!err) {
                err = document.createElement('div');
                err.id = id;
                err.className = 'char-limit-error';
                if (counter && counter.parentNode) {
                    counter.parentNode.insertBefore(err, counter);
                } else if (el.parentNode) {
                    el.parentNode.insertBefore(err, el.nextSibling);
                }
            }
            return err;
        }

        function refresh(el) {
            var max = getMax(el);
            if (!max) return;

            var len = (el.value || '').length;
            var counter = ensureCounter(el);
            var err = ensureError(el, counter);
            var overLimit = len >= max;

            counter.textContent = len + '/' + max;
            counter.classList.toggle('char-limit-danger', overLimit);

            if (overLimit) {
                el.classList.add('char-limit-exceeded', 'input-invalid');
                el.__charLimitInvalid = true;
                err.textContent = 'The ' + getLabel(el) + ' field can have a maximum of ' + max + ' characters.';
                err.style.display = 'block';
            } else {
                el.classList.remove('char-limit-exceeded');
                // Only remove 'input-invalid' if the char-limit guard was the
                // one who added it — a different validator (e.g. the bot's
                // own name/email checks) may own that class for its own
                // reason and must not be clobbered here.
                if (el.__charLimitInvalid) {
                    el.classList.remove('input-invalid');
                    el.__charLimitInvalid = false;
                }
                err.style.display = 'none';
            }
        }

        function wireField(el) {
            if (!el || el.__charLimitWired || !isTextLike(el) || !getMax(el)) return;
            el.__charLimitWired = true;
            refresh(el);
            el.addEventListener('input', function () { refresh(el); });
        }

        function collectGuardedFields(scope) {
            var root = scope || document;
            var found = root.querySelectorAll ? root.querySelectorAll(GUARD_SELECTOR) : [];
            return Array.prototype.filter.call(found, isTextLike);
        }

        function showFormAlert(form, message) {
            var box = form.querySelector('.char-limit-form-alert');
            if (!box) {
                box = document.createElement('div');
                box.className = 'char-limit-form-alert';
                form.insertBefore(box, form.firstChild);
            }
            box.textContent = message;
            box.style.display = 'block';
            clearTimeout(box.__hideTimer);
            box.__hideTimer = setTimeout(function () { box.style.display = 'none'; }, 6000);
        }

        // Returns true if the form is clean. If any guarded field is over its
        // limit, blocks the caller (return false) and clears ONLY those
        // field(s) — every other field on the form is left exactly as the
        // user left it — then shows a specific validation message near the
        // form instead of a generic error.
        function validateForm(form) {
            if (!form) return true;
            var fields = collectGuardedFields(form);
            var invalid = fields.filter(function (el) {
                var max = getMax(el);
                return max && (el.value || '').length >= max;
            });

            if (!invalid.length) return true;

            invalid.forEach(function (el) {
                el.value = '';
                refresh(el);
                el.classList.add('input-invalid');
            });

            showFormAlert(form, invalid.length === 1
                ? 'The ' + getLabel(invalid[0]) + ' field exceeded its character limit and has been cleared. Please re-enter it.'
                : 'Some fields exceeded their character limit and have been cleared. Please re-enter them.');

            invalid[0].focus();
            botShake();
            return false;
        }

        function wireForm(form) {
            if (!form || form.__charLimitFormWired) return;
            form.__charLimitFormWired = true;

            collectGuardedFields(form).forEach(wireField);

            // Capture phase so this runs before other submit handlers (e.g. the
            // bot's own submit-spin animation) get a chance to send the form.
            form.addEventListener('submit', function (e) {
                if (!validateForm(form)) {
                    e.preventDefault();
                    e.stopPropagation();
                    if (e.stopImmediatePropagation) e.stopImmediatePropagation();
                }
            }, true);
        }

        function init(scope) {
            var target = scope ? (typeof scope === 'string' ? document.querySelector(scope) : scope) : document;
            if (!target) return;

            var fields = collectGuardedFields(target);
            fields.forEach(wireField);

            var seen = [];
            fields.forEach(function (el) {
                var f = el.form || (el.closest ? el.closest('form') : null);
                if (f && seen.indexOf(f) === -1) {
                    seen.push(f);
                    wireForm(f);
                }
            });
        }

        return { init: init, wireForm: wireForm, wireField: wireField, refresh: refresh, validateForm: validateForm };
    })();

    // =========================================================================
    // SIDEBAR BOT TIPS (animated tooltip cycling)
    // =========================================================================

    function initSidebarBotTips() {
        var container = document.querySelector('.sidebar-bot-container');
        if (!container) return;

        var tips = [
            "Click me to ask IT questions! \ud83d\udcac",
            "How to raise a complaint? Ask me!",
            "Check ticket status here \ud83c\udfab",
            "I know all IT issue types \ud83d\udcbb",
            "Ask about hardware or software help \ud83d\udd27"
        ];
        var tooltip = container.querySelector('.sidebar-bot-tooltip');
        var botEl = container.querySelector('.bot-sidebar-avatar');
        var i = 0;

        setInterval(function () {
            if (tooltip) {
                tooltip.style.opacity = '0';
                setTimeout(function () {
                    tooltip.textContent = tips[i % tips.length];
                    tooltip.style.opacity = '1';
                    i++;
                }, 300);
            }
            if (botEl) {
                botEl.classList.remove('sidebar-bot-pulse');
                void botEl.offsetWidth;
                botEl.classList.add('sidebar-bot-pulse');
            }
        }, 6000);

        var botAvatar = container.querySelector('.bot-sidebar-avatar');
        if (botAvatar) {
            botAvatar.addEventListener('mouseenter', function () {
                var mouth = botAvatar.querySelector('.bot-mouth');
                var face = botAvatar.querySelector('.bot-face');
                if (mouth) {
                    mouth.style.borderRadius = '0 0 12px 12px';
                    mouth.style.height = '8px';
                    mouth.style.background = 'var(--accent-primary)';
                    mouth.style.border = 'none';
                }
                if (face) {
                    face.style.position = 'relative';
                    if (!document.getElementById('botBlushLeft')) {
                        var bl = document.createElement('div');
                        bl.id = 'botBlushLeft';
                        bl.style.cssText = 'position:absolute;width:10px;height:6px;background:rgba(255,150,150,0.6);border-radius:50%;bottom:6px;left:4px;transition:opacity 0.3s;';
                        var br = document.createElement('div');
                        br.id = 'botBlushRight';
                        br.style.cssText = 'position:absolute;width:10px;height:6px;background:rgba(255,150,150,0.6);border-radius:50%;bottom:6px;right:4px;transition:opacity 0.3s;';
                        face.appendChild(bl);
                        face.appendChild(br);
                    } else {
                        document.getElementById('botBlushLeft').style.opacity = '1';
                        document.getElementById('botBlushRight').style.opacity = '1';
                    }
                }
            });

            botAvatar.addEventListener('mouseleave', function () {
                var mouth = botAvatar.querySelector('.bot-mouth');
                if (mouth) {
                    mouth.style.borderRadius = '';
                    mouth.style.height = '';
                    mouth.style.background = '';
                    mouth.style.border = '';
                }
                var bl = document.getElementById('botBlushLeft');
                var br = document.getElementById('botBlushRight');
                if (bl) bl.style.opacity = '0';
                if (br) br.style.opacity = '0';
            });
        }
    }

    // =========================================================================
    // FAQ CHAT PANEL
    // =========================================================================

    function initFAQChat() {
        var container = document.querySelector('.sidebar-bot-container');
        if (!container) return;

        var greetingWords = ['hi', 'hello', 'hey', 'good', 'morning', 'afternoon', 'evening', 'howdy', 'hii', 'helo', 'greetings', 'thanks', 'thank'];

        function isGreeting(text) {
            var words = text.toLowerCase().replace(/[^a-z ]/g, ' ').trim().split(/\s+/);
            var matched = 0;
            for (var i = 0; i < words.length; i++) {
                if (greetingWords.indexOf(words[i]) !== -1) matched++;
            }
            return matched > 0 && words.length <= 5;
        }

        var panel = document.createElement('div');
        panel.id = 'faqChatPanel';
        panel.style.cssText =
            'display:none;position:fixed;bottom:90px;left:20px;width:330px;' +
            'background:var(--bg-secondary);border:1.5px solid var(--accent-primary);' +
            'border-radius:14px;z-index:99999;flex-direction:column;overflow:hidden;' +
            'box-shadow:0 8px 32px rgba(0,0,0,0.25);';

        panel.innerHTML =
            '<div style="background:var(--accent-primary);color:#fff;padding:12px 16px;' +
                'display:flex;align-items:center;justify-content:space-between;' +
                'font-weight:600;font-size:14px;font-family:inherit;">' +
                '<span>&#129302; Simplify Assistant</span>' +
                '<span id="faqPanelClose" style="cursor:pointer;font-size:20px;line-height:1;opacity:.85;padding:0 4px;">&#x2715;</span>' +
            '</div>' +
            '<div id="faqChatMsgs" style="overflow-y:auto;padding:12px;display:flex;' +
                'flex-direction:column;gap:8px;font-size:13px;line-height:1.5;' +
                'max-height:280px;min-height:60px;font-family:inherit;"></div>' +
            '<div id="faqChips" style="padding:0 12px 8px;display:flex;flex-wrap:wrap;gap:5px;"></div>' +
            '<div style="padding:8px 10px;border-top:1px solid rgba(128,128,128,0.2);' +
                'display:flex;gap:8px;align-items:center;">' +
                '<input id="faqChatInput" type="text" placeholder="Type your question..." maxlength="300" ' +
                    'style="flex:1;padding:8px 12px;border:1px solid rgba(128,128,128,0.3);' +
                    'border-radius:20px;font-size:13px;background:var(--bg-primary,#fff);' +
                    'color:var(--text-primary,#000);outline:none;font-family:inherit;" />' +
                // FIX 1: Added type="button" — without this the button defaults to type="submit",
                // which submits any surrounding page form and aborts the XHR mid-flight (Error 0).
                '<button id="faqChatSend" type="button" style="background:var(--accent-primary);color:#fff;' +
                    'border:none;border-radius:50%;width:36px;height:36px;cursor:pointer;' +
                    'font-size:18px;flex-shrink:0;display:flex;align-items:center;justify-content:center;">' +
                    '&#10148;' +
                '</button>' +
            '</div>';

        document.body.appendChild(panel);

        container.style.cursor = 'pointer';
        container.addEventListener('click', function () {
            var isVisible = panel.style.display === 'flex';
            panel.style.display = isVisible ? 'none' : 'flex';
            if (!isVisible) {
                var msgs = document.getElementById('faqChatMsgs');
                if (!msgs.children.length) {
                    addBotMsg('Hi! I\'m your IT Support Assistant. Ask me about raising complaints, ticket status, or IT issues.');
                    // FIX 2: Chip labels now use the EXACT question text from the FAQs table.
                    // The search does a LIKE match on Question/Keywords/Answer.
                    // When the chip text matches the FAQ Question exactly, SQL always finds
                    // the right record. Vague paraphrases like "How to raise a complaint?"
                    // matched multiple FAQs and returned the wrong one due to random ordering.
                    showChips([
                        'How do I raise a new complaint?',
                        'What types of complaints can I raise?',
                        'What do the different ticket statuses mean?'
                    ]);
                }
                setTimeout(function () { document.getElementById('faqChatInput').focus(); }, 100);
            }
        });

        document.getElementById('faqPanelClose').addEventListener('click', function (e) {
            e.stopPropagation();
            panel.style.display = 'none';
        });

        function sendMsg() {
            var input = document.getElementById('faqChatInput');
            var text = (input.value || '').trim();
            if (!text) return;

            clearChips();
            addUserMsg(text);
            input.value = '';

            if (isGreeting(text)) {
                setTimeout(function () {
                    addBotMsg('Hello! How can I help? Ask me anything about the Ticket Management System.');
                    // FIX 2 (same): exact FAQ question text for greeting chips
                    showChips([
                        'How do I raise a new complaint?',
                        'What types of complaints can I raise?',
                        'What do the different ticket statuses mean?'
                    ]);
                }, 300);
                return;
            }

            var typingEl = addBotMsg('Searching FAQs\u2026');
            typingEl.style.fontStyle = 'italic';
            typingEl.style.opacity = '0.6';

            var xhr = new XMLHttpRequest();
            xhr.open('POST', '/ChatBot/SearchFAQ', true);
            xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
            xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
            xhr.onreadystatechange = function () {
                if (xhr.readyState !== 4) return;
                if (typingEl && typingEl.parentNode) typingEl.parentNode.removeChild(typingEl);

                if (xhr.status === 200) {
                    try {
                        var res = JSON.parse(xhr.responseText);
                        if (res.success && res.data && res.data.length > 0) {
                            addBotMsg(res.data[0].Answer);
                            if (res.data.length > 1) {
                                var chips = [];
                                for (var i = 1; i < Math.min(res.data.length, 3) ; i++) {
                                    chips.push(res.data[i].Question);
                                }
                                showChips(chips);
                            }
                        } else {
                            addBotMsg('I couldn\'t find an answer. Try rephrasing, or contact IT Support directly.');
                            // FIX 2 (same): exact FAQ question text for fallback chips
                            showChips([
                                'How do I raise a new complaint?',
                                'My computer won\'t turn on. What should I do?',
                                'An application won\'t install on my computer.'
                            ]);
                        }
                    } catch (e) {
                        addBotMsg('Could not read the response. Please try again.');
                    }
                } else if (xhr.status === 401) {
                    addBotMsg('Your session has expired. Please refresh the page and log in again.');
                } else if (xhr.status === 0) {
                    // FIX 3: status 0 means the request was aborted before the server responded.
                    // This happens when type="button" was missing and the send button accidentally
                    // submitted a page form, causing navigation mid-request.
                    // With FIX 1 applied this path should no longer be hit, but we show a
                    // clear message instead of the raw "Error 0" the user saw before.
                    addBotMsg('Connection interrupted. Please try again.');
                } else {
                    addBotMsg('Something went wrong (HTTP ' + xhr.status + '). Please try again or contact IT Support.');
                }
            };
            xhr.send('searchText=' + encodeURIComponent(text));
        }

        document.getElementById('faqChatSend').addEventListener('click', sendMsg);
        document.getElementById('faqChatInput').addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); sendMsg(); }
        });

        function addUserMsg(text) {
            var msgs = document.getElementById('faqChatMsgs');
            var d = document.createElement('div');
            d.style.cssText =
                'background:var(--accent-primary);color:#fff;padding:8px 12px;' +
                'border-radius:12px 12px 4px 12px;align-self:flex-end;' +
                'max-width:85%;word-break:break-word;';
            d.textContent = text;
            msgs.appendChild(d);
            msgs.scrollTop = msgs.scrollHeight;
        }

        function addBotMsg(text) {
            var msgs = document.getElementById('faqChatMsgs');
            var d = document.createElement('div');
            d.style.cssText =
                'background:var(--bg-tertiary,#f0f2f5);color:var(--text-primary,#000);' +
                'padding:8px 12px;border-radius:12px 12px 12px 4px;align-self:flex-start;' +
                'max-width:85%;word-break:break-word;';
            d.textContent = text;
            msgs.appendChild(d);
            msgs.scrollTop = msgs.scrollHeight;
            return d;
        }

        function showChips(questions) {
            var chipsDiv = document.getElementById('faqChips');
            if (!chipsDiv) return;
            chipsDiv.innerHTML = '';
            for (var i = 0; i < questions.length; i++) {
                (function (q) {
                    var chip = document.createElement('button');
                    // FIX 1 (chips): same as the Send button — must be type="button" so clicking
                    // a chip never submits a page form and aborts the XHR.
                    chip.setAttribute('type', 'button');
                    chip.style.cssText =
                        'background:transparent;color:var(--accent-primary);' +
                        'border:1px solid var(--accent-primary);border-radius:20px;' +
                        'padding:3px 10px;font-size:11px;cursor:pointer;font-family:inherit;';
                    chip.textContent = q;
                    chip.addEventListener('click', function (e) {
                        e.preventDefault();
                        e.stopPropagation();
                        document.getElementById('faqChatInput').value = q;
                        sendMsg();
                    });
                    chipsDiv.appendChild(chip);
                })(questions[i]);
            }
        }

        function clearChips() {
            var c = document.getElementById('faqChips');
            if (c) c.innerHTML = '';
        }
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    window.AssistantBot = {
        CharLimitGuard: CharLimitGuard,
        init: function (formSelector) {
            createBotDOM();
            wireFieldListeners(formSelector);
            wireSubmitSpin(formSelector);
            CharLimitGuard.init(formSelector);

            document.querySelectorAll('a').forEach(function (link) {
                link.addEventListener('click', function (e) {
                    var href = this.getAttribute('href');
                    if (!href || href === '#' || href.indexOf('javascript') === 0) return;
                    e.preventDefault();
                    var container = document.querySelector('.auth-page-enter');
                    if (container) {
                        container.classList.add('auth-page-exit');
                        setTimeout(function () { window.location.href = href; }, 350);
                    } else {
                        window.location.href = href;
                    }
                });
            });

            initSidebarBotTips();
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        initSidebarBotTips();
        initFAQChat();
        // Safety net: guards every maxlength / data-max-length field on the
        // page even on views that never call AssistantBot.init(formSelector).
        CharLimitGuard.init(document);
    });

})(window, document);