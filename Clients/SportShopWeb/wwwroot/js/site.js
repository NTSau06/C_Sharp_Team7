/* Sport Shop - script phía khách hàng (không dùng thư viện ngoài) */
(function () {
    'use strict';

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $all(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function token() {
        var el = $('#af-token input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }
    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    // ------------------------------------------------------------ thông báo nhỏ (toast)
    var toastTimer;
    function toast(msg, bad) {
        var t = $('#toast');
        if (!t) { alert(msg); return; }
        t.textContent = msg;
        t.className = 'toast' + (bad ? ' toast-bad' : '');
        t.hidden = false;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { t.hidden = true; }, 3200);
    }

    function setCartCount(n) {
        var b = $('#cart-count');
        if (!b) return;
        b.textContent = n;
        b.style.display = n > 0 ? '' : 'none';
    }

    // ------------------------------------------------------------ menu tài khoản
    $all('[data-toggle="dropdown"]').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            btn.parentNode.classList.toggle('open');
        });
    });
    document.addEventListener('click', function () {
        $all('.dropdown.open').forEach(function (d) { d.classList.remove('open'); });
    });

    // ------------------------------------------------------------ tìm kiếm + gợi ý AJAX (UC 1.2.8.1, 1.2.8.2)
    var form = $('#search-form'), input = $('#search-input'), box = $('#search-suggest'), msg = $('#search-msg');
    if (form && input && box) {
        var timer, lastQ = '', active = -1;

        form.addEventListener('submit', function (e) {
            if (input.value.trim() === '') {
                e.preventDefault();
                msg.hidden = false;
                input.focus();
                setTimeout(function () { msg.hidden = true; }, 2500);
            }
        });

        function hide() { box.hidden = true; active = -1; }

        function render(items, q) {
            if (!items.length) { hide(); return; }   // không khớp => không hiện bảng gợi ý
            var html = items.map(function (it) {
                var img = '<span class="thumb thumb-xs" style="--c:' + esc(it.mau) + '"><span class="thumb-txt">' + esc(it.vietTat) + '</span>' +
                    (it.anh ? '<img src="' + esc(it.anh) + '" alt="" onerror="this.remove()">' : '') + '</span>';
                return '<a class="sg-item" href="' + esc(it.url) + '">' + img +
                    '<span class="sg-text"><span class="sg-name">' + esc(it.tenSP) + '</span><span class="sg-cat">' + esc(it.tenDM) + '</span></span>' +
                    '<span class="sg-price">' + esc(it.gia) + '</span></a>';
            }).join('');
            html += '<a class="sg-all" href="' + form.getAttribute('action') + '?q=' + encodeURIComponent(q) + '">Xem tất cả kết quả cho "' + esc(q) + '" ›</a>';
            box.innerHTML = html;
            box.hidden = false;
            active = -1;
        }

        input.addEventListener('input', function () {
            var q = input.value.trim();
            msg.hidden = true;
            clearTimeout(timer);
            if (!q) { hide(); lastQ = ''; return; }
            timer = setTimeout(function () {
                if (q === lastQ) { box.hidden = !box.innerHTML; return; }
                lastQ = q;
                fetch('/Search/Suggest?q=' + encodeURIComponent(q), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                    .then(function (r) { return r.ok ? r.json() : []; })
                    .then(function (items) { if (input.value.trim() === q) render(items, q); })
                    .catch(hide);
            }, 200);
        });

        input.addEventListener('keydown', function (e) {
            var links = $all('a', box);
            if (box.hidden || !links.length) return;
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                e.preventDefault();
                active = (active + (e.key === 'ArrowDown' ? 1 : -1) + links.length) % links.length;
                links.forEach(function (a, i) { a.classList.toggle('active', i === active); });
            } else if (e.key === 'Enter' && active >= 0) {
                e.preventDefault();
                location.href = links[active].href;
            } else if (e.key === 'Escape') {
                hide();
            }
        });
        input.addEventListener('focus', function () { if (box.innerHTML && input.value.trim()) box.hidden = false; });
        document.addEventListener('click', function (e) { if (!form.contains(e.target)) hide(); });
    }

    // ------------------------------------------------------------ nút +/- số lượng
    $all('.qty-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var inp = $('input[type="number"]', btn.parentNode);
            if (!inp || inp.disabled) return;
            var min = parseInt(inp.min || '0', 10), max = parseInt(inp.max || '999', 10);
            var v = (parseInt(inp.value, 10) || 0) + parseInt(btn.getAttribute('data-step'), 10);
            inp.value = Math.max(min, Math.min(max, v));
            inp.dispatchEvent(new Event('change', { bubbles: true }));
        });
    });

    // Giỏ hàng: tự gửi form cập nhật khi đổi số lượng
    $all('.js-qty-form').forEach(function (f) {
        var t;
        f.addEventListener('change', function () {
            clearTimeout(t);
            t = setTimeout(function () { f.submit(); }, 500);
        });
    });

    // ------------------------------------------------------------ thêm vào giỏ (AJAX)
    $all('form.js-add-cart').forEach(function (f) {
        f.addEventListener('submit', function (e) {
            e.preventDefault();
            var btn = $('button[type="submit"]', f);
            if (btn) btn.disabled = true;
            fetch(f.action, {
                method: 'POST',
                body: new FormData(f),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(function (r) { return r.json(); })
                .then(function (d) {
                    if (d.needLogin) { location.href = d.url; return; }
                    toast(d.message, !d.ok);
                    if (typeof d.count === 'number') setCartCount(d.count);
                })
                .catch(function () { f.submit(); })
                .then(function () { if (btn) btn.disabled = false; });
        });
    });

    // ------------------------------------------------------------ áp dụng mã giảm giá (UC 1.2.14)
    var vBtn = $('#voucher-apply'), vInput = $('#voucher-code'), vMsg = $('#voucher-msg');
    function applyVoucher() {
        var fd = new FormData();
        fd.append('code', vInput.value);
        fd.append('__RequestVerificationToken', token());
        vBtn.disabled = true;
        fetch(vBtn.getAttribute('data-url'), { method: 'POST', body: fd, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.json(); })
            .then(function (d) {
                vMsg.textContent = d.thongBao;
                vMsg.className = 'voucher-msg ' + (d.hopLe ? 'ok' : 'bad');
                if (d.hopLe && d.code) vInput.value = d.code;
                var dis = $('#sum-discount'), tot = $('#sum-total');
                if (dis) dis.textContent = '- ' + d.tienGiam;
                if (tot) tot.textContent = d.tongCong;
            })
            .catch(function () { vMsg.textContent = 'Không áp dụng được mã. Vui lòng thử lại.'; vMsg.className = 'voucher-msg bad'; })
            .then(function () { vBtn.disabled = false; });
    }
    if (vBtn && vInput) {
        vBtn.addEventListener('click', applyVoucher);
        vInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); applyVoucher(); }
        });
        $all('.js-voucher-chip').forEach(function (c) {
            c.addEventListener('click', function () { vInput.value = c.getAttribute('data-code'); applyVoucher(); });
        });
    }

    // ------------------------------------------------------------ bộ lọc / sắp xếp
    $all('[data-autosubmit]').forEach(function (s) {
        s.addEventListener('change', function () { if (s.form) s.form.submit(); });
    });
    $all('[data-price]').forEach(function (a) {
        a.addEventListener('click', function (e) {
            e.preventDefault();
            var f = $('#filter-form');
            if (!f) return;
            var p = a.getAttribute('data-price').split('-');
            f.elements['GiaTu'].value = p[0] || '';
            f.elements['GiaDen'].value = p[1] || '';
            f.submit();
        });
    });
})();
