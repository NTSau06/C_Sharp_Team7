/* SportZone - script phía khách hàng (không dùng thư viện ngoài) */
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
    // kieu: false/undefined = thành công (xanh lá), true = lỗi (đỏ), 'warn' = cảnh báo (vàng nhạt)
    function toast(msg, kieu) {
        var t = $('#toast');
        if (!t) { alert(msg); return; }
        t.textContent = msg;
        t.className = 'toast' + (kieu === 'warn' ? ' toast-warn' : (kieu ? ' toast-bad' : ''));
        t.hidden = false;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { t.hidden = true; }, 3600);
    }
    var ANH_MAC_DINH = '/images/placeholders/no-image.svg';

    function setCartCount(n) {
        var b = $('#cart-count');
        if (!b) return;
        b.textContent = n;
        b.style.display = n > 0 ? '' : 'none';
    }

    // ------------------------------------------------------------ menu thả xuống (danh mục, tài khoản)
    function closeDropdowns(except) {
        $all('.dropdown.open').forEach(function (d) { if (d !== except) d.classList.remove('open'); });
    }
    $all('[data-toggle="dropdown"]').forEach(function (btn) {
        btn.setAttribute('aria-expanded', 'false');
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            var dd = btn.parentNode;
            closeDropdowns(dd);
            var open = dd.classList.toggle('open');
            btn.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
    });
    document.addEventListener('click', function (e) {
        if (!e.target.closest || !e.target.closest('.dropdown-menu')) closeDropdowns(null);
    });

    // ------------------------------------------------------------ nút ☰ trên điện thoại (W1 - thành phần 6)
    var navBtn = $('#nav-toggle'), mobileNav = $('#mobile-nav');
    function setMobileNav(open) {
        if (!navBtn || !mobileNav) return;
        mobileNav.hidden = !open;
        navBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
        navBtn.setAttribute('aria-label', open ? 'Đóng menu' : 'Mở menu');
        navBtn.textContent = open ? '✕' : '☰';
        document.body.classList.toggle('nav-open', open);
    }
    if (navBtn && mobileNav) {
        navBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            setMobileNav(mobileNav.hidden);
        });
        $all('a', mobileNav).forEach(function (a) { a.addEventListener('click', function () { setMobileNav(false); }); });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') { setMobileNav(false); closeDropdowns(null); }
        });
        // Xoay ngang / phóng to màn hình > 768px thì đóng menu điện thoại
        window.addEventListener('resize', function () { if (window.innerWidth > 768) setMobileNav(false); });
    }

    // ------------------------------------------------------------ sao chép mã giảm giá (trang chủ W1)
    function copyText(text) {
        if (navigator.clipboard && window.isSecureContext) return navigator.clipboard.writeText(text);
        return new Promise(function (resolve, reject) {
            var ta = document.createElement('textarea');
            ta.value = text; ta.setAttribute('readonly', ''); ta.style.position = 'fixed'; ta.style.opacity = '0';
            document.body.appendChild(ta); ta.select();
            try { document.execCommand('copy') ? resolve() : reject(); } catch (err) { reject(err); }
            document.body.removeChild(ta);
        });
    }
    $all('[data-copy]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var code = btn.getAttribute('data-copy');
            copyText(code)
                .then(function () {
                    toast('Đã sao chép mã ' + code + '. Dán mã ở bước Giỏ hàng / Đặt hàng để được giảm giá.');
                    var old = btn.textContent;
                    btn.textContent = '✓ Đã sao chép';
                    setTimeout(function () { btn.textContent = old; }, 1800);
                })
                .catch(function () { toast('Không sao chép được. Mã của bạn: ' + code, 'warn'); });
        });
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
                var src = it.anh || ANH_MAC_DINH;
                var img = '<span class="thumb thumb-xs" style="--c:' + esc(it.mau) + '"><span class="thumb-txt">' + esc(it.vietTat) + '</span>' +
                    '<img src="' + esc(src) + '" alt=""' + (src === ANH_MAC_DINH ? ' data-fb="1"' : '') +
                    ' onerror="if(this.dataset.fb){this.remove();}else{this.dataset.fb=\'1\';this.src=\'' + ANH_MAC_DINH + '\';}"></span>';
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

    // ------------------------------------------------------------ nút +/- số lượng + cảnh báo vượt tồn kho (TC21)
    function qtyWarn(inp, msg) {
        var sel = inp.getAttribute('data-warn'), box = sel ? $(sel) : null;
        if (box) { box.textContent = msg || ''; box.hidden = !msg; }
        if (msg) toast(msg, 'warn');
    }
    function msgVuotTon(inp) {
        var ton = parseInt(inp.getAttribute('data-ton') || inp.max || '0', 10);
        var max = parseInt(inp.max || '0', 10);
        return ton > max
            ? 'Mỗi sản phẩm chỉ đặt tối đa ' + max + ' (giới hạn mỗi đơn hàng)'
            : 'Số lượng vượt quá tồn kho hiện tại (chỉ còn ' + ton + ' sản phẩm)';
    }
    /** Đưa giá trị về [min, max]; trả về true nếu giá trị thay đổi. Vượt max => hiển thị cảnh báo (không chặn im lặng). */
    function clampQty(inp, v) {
        var min = parseInt(inp.min || '0', 10), max = parseInt(inp.max || '999', 10);
        var old = inp.value;
        if (isNaN(v)) v = min;
        if (v > max) { qtyWarn(inp, msgVuotTon(inp)); v = max; }
        else { qtyWarn(inp, ''); }
        if (v < min) v = min;
        inp.value = v;
        return String(v) !== String(old);
    }
    $all('.qty-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var inp = $('input[type="number"]', btn.parentNode);
            if (!inp || inp.disabled) return;
            var v = (parseInt(inp.value, 10) || 0) + parseInt(btn.getAttribute('data-step'), 10);
            if (clampQty(inp, v)) inp.dispatchEvent(new Event('qtychange', { bubbles: true }));
        });
    });
    // Gõ trực tiếp số lượng: kiểm tra khi rời ô / nhấn Enter
    $all('.qty input[type="number"]').forEach(function (inp) {
        inp.addEventListener('change', function () {
            clampQty(inp, parseInt(inp.value, 10));
            inp.dispatchEvent(new Event('qtychange', { bubbles: true }));
        });
    });

    // Giỏ hàng: tự gửi form cập nhật khi đổi số lượng
    $all('.js-qty-form').forEach(function (f) {
        var t;
        f.addEventListener('qtychange', function () {
            clearTimeout(t);
            t = setTimeout(function () { f.submit(); }, 700);
        });
        f.addEventListener('submit', function () { clearTimeout(t); });
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

    // ------------------------------------------------------------ "Mua ngay" ở trang chi tiết: lấy số lượng đang chọn
    $all('form.js-buy-now').forEach(function (f) {
        f.addEventListener('submit', function () {
            var qty = $('form.js-add-cart input[name="soLuong"]');
            var target = $('input[name="soLuong"]', f);
            if (qty && target) target.value = qty.value;
        });
    });

    // ------------------------------------------------------------ trang chi tiết: tính tạm tính động, tab & phóng to ảnh
    var detailQty = $('#detail-qty');
    if (detailQty) {
        function updateDetailSubtotal() {
            var sel = detailQty.getAttribute('data-subtotal');
            var out = sel ? $(sel) : null;
            var unit = parseFloat(detailQty.getAttribute('data-unit-price') || '0');
            var q = Math.max(1, parseInt(detailQty.value, 10) || 1);
            if (out && unit > 0) {
                out.textContent = Math.round(unit * q).toLocaleString('vi-VN') + ' ₫';
            }
        }
        detailQty.addEventListener('qtychange', updateDetailSubtotal);
        detailQty.addEventListener('input', updateDetailSubtotal);
        detailQty.addEventListener('change', updateDetailSubtotal);
    }

    $all('[data-tabs]').forEach(function (wrap) {
        var btns = $all('.tab-btn', wrap);
        var panels = $all('.tab-panel', wrap);
        btns.forEach(function (btn) {
            btn.addEventListener('click', function () {
                var targetId = btn.getAttribute('data-tab');
                btns.forEach(function (b) {
                    var on = b === btn;
                    b.classList.toggle('active', on);
                    b.setAttribute('aria-selected', on ? 'true' : 'false');
                });
                panels.forEach(function (p) { p.hidden = p.id !== targetId; });
            });
        });
    });

    var lightbox = $('#img-lightbox'), lbImg = $('#lightbox-img'), lbCap = $('#lightbox-cap');
    if (lightbox && lbImg) {
        function closeLightbox() { lightbox.hidden = true; document.body.style.overflow = ''; }
        $all('.js-zoom-trigger').forEach(function (stage) {
            function openZoom() {
                var img = $('img', stage);
                if (!img) return;
                lbImg.src = img.currentSrc || img.src;
                lbImg.alt = img.alt || '';
                if (lbCap) lbCap.textContent = img.alt || '';
                lightbox.hidden = false;
                document.body.style.overflow = 'hidden';
            }
            stage.addEventListener('click', openZoom);
            stage.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); openZoom(); }
            });
        });
        var lbClose = $('.lightbox-close', lightbox);
        if (lbClose) lbClose.addEventListener('click', closeLightbox);
        lightbox.addEventListener('click', function (e) { if (e.target === lightbox) closeLightbox(); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && !lightbox.hidden) closeLightbox(); });
    }

    // ------------------------------------------------------------ áp dụng mã giảm giá (UC 1.2.14)
    var vBtn = $('#voucher-apply'), vInput = $('#voucher-code'), vMsg = $('#voucher-msg'), vRemove = $('#voucher-remove');
    function setTotals(d) {
        var dis = $('#sum-discount'), tot = $('#sum-total');
        if (dis && d.tienGiam) dis.textContent = '- ' + d.tienGiam;
        if (tot && d.tongCong) tot.textContent = d.tongCong;
    }
    // Nút "Bỏ mã" (giỏ hàng W4 + đặt hàng W5): xóa mã khỏi Session, trả lại tổng tiền ban đầu
    if (vRemove) {
        vRemove.addEventListener('click', function () {
            var fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            vRemove.disabled = true;
            fetch(vRemove.getAttribute('data-url'), { method: 'POST', body: fd, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (r) { return r.json(); })
                .then(function (d) {
                    if (vInput) vInput.value = '';
                    if (vMsg) { vMsg.textContent = d.thongBao || ''; vMsg.className = 'voucher-msg ok'; }
                    setTotals(d);
                    vRemove.hidden = true;
                    var err = $('[data-valmsg-for="MaGiamGia"]');
                    if (err) err.textContent = '';
                })
                .catch(function () { toast('Không bỏ được mã. Vui lòng thử lại.', true); })
                .then(function () { vRemove.disabled = false; });
        });
    }
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
                setTotals(d);
                if (vRemove) vRemove.hidden = !d.hopLe;
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

    // ------------------------------------------------------------ chọn thông tin giao hàng khi thanh toán (tài khoản / địa chỉ khác)
    var shipSection = $('#ship-section');
    if (shipSection) {
        var shipRadios = $all('input[name="ChonDiaChi"]', shipSection);
        var shipFields = $('#ship-fields');
        var inpName = $('#HoTenNhan'), inpPhone = $('#SDT'), inpAddr = $('#DiaChiGiao'), chkSave = $('#LuuDiaChiMacDinh');
        var customDraft = {
            name: (shipRadios.some(function (r) { return r.checked && r.value === 'other'; }) && inpName) ? inpName.value : '',
            phone: (shipRadios.some(function (r) { return r.checked && r.value === 'other'; }) && inpPhone) ? inpPhone.value : '',
            addr: (shipRadios.some(function (r) { return r.checked && r.value === 'other'; }) && inpAddr) ? inpAddr.value : ''
        };
        var currentChoice = (shipRadios.find(function (r) { return r.checked; }) || {}).value || 'account';

        function clearShipErrors() {
            $all('.field-err', shipFields).forEach(function (el) { el.textContent = ''; });
            $all('.input-validation-error', shipFields).forEach(function (el) { el.classList.remove('input-validation-error'); });
        }

        function applyShipChoice(radio, fromUser) {
            if (!radio || !shipFields) return;
            if (fromUser && currentChoice === 'other') {
                customDraft.name = inpName ? inpName.value : '';
                customDraft.phone = inpPhone ? inpPhone.value : '';
                customDraft.addr = inpAddr ? inpAddr.value : '';
            }
            currentChoice = radio.value;

            if (radio.value === 'other') {
                if (fromUser) {
                    if (inpName) inpName.value = customDraft.name;
                    if (inpPhone) inpPhone.value = customDraft.phone;
                    if (inpAddr) inpAddr.value = customDraft.addr;
                    if (chkSave) chkSave.checked = false;
                    clearShipErrors();
                }
                shipFields.hidden = false;
                if (fromUser && inpName) inpName.focus();
            } else {
                var hasAddr = radio.getAttribute('data-has-addr') === '1';
                if (fromUser) {
                    if (inpName) inpName.value = radio.getAttribute('data-name') || '';
                    if (inpPhone) inpPhone.value = radio.getAttribute('data-phone') || '';
                    if (inpAddr) inpAddr.value = radio.getAttribute('data-addr') || '';
                    if (chkSave) chkSave.checked = !hasAddr;
                    clearShipErrors();
                }
                shipFields.hidden = hasAddr && $all('.field-err', shipFields).every(function (el) { return !el.textContent.trim(); });
                if (fromUser && !hasAddr && inpAddr) inpAddr.focus();
            }
        }

        shipRadios.forEach(function (r) {
            r.addEventListener('change', function () {
                if (r.checked) applyShipChoice(r, true);
            });
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


// ---------------------------------------------------------------- Đặt hàng: chọn địa chỉ từ sổ địa chỉ
(function () {
    var sel = document.getElementById('chon-dia-chi');
    if (!sel) return;
    sel.addEventListener('change', function () {
        var o = sel.options[sel.selectedIndex];
        if (!o || !o.value) return;
        var map = { HoTenNhan: o.dataset.ten, SDT: o.dataset.sdt, DiaChiGiao: o.dataset.dc };
        Object.keys(map).forEach(function (id) {
            var el = document.getElementById(id);
            if (el) el.value = map[id] || '';
        });
    });
})();
