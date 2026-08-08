// ============================================================================
// KopiYo - layar kasir
//
// PENTING: semua angka yang dihitung di file ini HANYA untuk tampilan.
// Server menghitung ulang seluruh harga saat checkout dari data database, dan
// payload checkout sengaja tidak memuat satu pun field harga. Jadi mengubah
// harga lewat DevTools tidak berpengaruh apa pun pada total yang tersimpan.
// ============================================================================

(function () {
    'use strict';

    let catalog = null;
    let activeCategoryId = null;
    let nextLineId = 1;
    let pendingProduct = null;   // produk yang sedang dipilih variannya di modal

    const cart = {
        lines: [],
        discountPercent: 0,
        paymentMethod: 'Cash',
        amountPaid: 0
    };

    const $ = id => document.getElementById(id);
    const rupiah = n => 'Rp ' + Math.round(n).toLocaleString('id-ID');

    /**
     * Dua baris keranjang digabung hanya kalau produk, kombinasi varian, DAN
     * catatannya sama persis. Varian diurutkan dulu supaya [3,5] dan [5,3]
     * dianggap identik.
     */
    const lineKey = l =>
        l.productId + '|' + [...l.variantOptionIds].sort((a, b) => a - b).join(',') + '|' + (l.note || '');

    // ---- Pemanggilan API ---------------------------------------------------

    function antiforgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    async function apiGet(url) {
        const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
        if (res.status === 401) { location.href = '/Account/Login'; throw new Error('unauthorized'); }
        if (!res.ok) throw new Error('Gagal memuat data (' + res.status + ')');
        return res.json();
    }

    // ---- Render katalog ----------------------------------------------------

    function renderCategories() {
        const box = $('category-list');
        box.innerHTML = '';

        const makeButton = (id, label) => {
            const a = document.createElement('button');
            a.type = 'button';
            a.className = 'list-group-item list-group-item-action' +
                (activeCategoryId === id ? ' active' : '');
            a.textContent = label;
            a.onclick = () => { activeCategoryId = id; renderCategories(); renderProducts(); };
            return a;
        };

        box.appendChild(makeButton(null, 'Semua'));
        catalog.categories.forEach(c => box.appendChild(makeButton(c.id, c.name)));
    }

    function renderProducts() {
        const grid = $('product-grid');
        const term = $('product-search').value.trim().toLowerCase();
        grid.innerHTML = '';

        const visible = catalog.products.filter(p =>
            (activeCategoryId === null || p.categoryId === activeCategoryId) &&
            (term === '' || p.name.toLowerCase().includes(term)));

        $('product-empty').classList.toggle('d-none', visible.length > 0);

        visible.forEach(p => {
            const col = document.createElement('div');
            col.className = 'col-6 col-xl-4';
            col.innerHTML =
                '<button type="button" class="btn btn-outline-dark w-100 h-100 product-card">' +
                '<span class="product-name"></span>' +
                '<span class="product-price"></span>' +
                '</button>';
            col.querySelector('.product-name').textContent = p.name;
            col.querySelector('.product-price').textContent = rupiah(p.basePrice);
            col.querySelector('button').onclick = () => onProductClick(p);
            grid.appendChild(col);
        });
    }

    // ---- Pemilihan varian --------------------------------------------------

    function groupsFor(product) {
        // Lookup, bukan pencarian: katalog mengirim grup varian sekali secara datar
        // dan produk hanya menyimpan id-nya.
        return product.variantGroupIds
            .map(id => catalog.variantGroups.find(g => g.id === id))
            .filter(Boolean);
    }

    function onProductClick(product) {
        const groups = groupsFor(product);
        if (groups.length === 0) {
            addLine(product, [], '');
            return;
        }
        openVariantModal(product, groups);
    }

    function openVariantModal(product, groups) {
        pendingProduct = product;
        $('variant-title').textContent = product.name;
        $('variant-note').value = '';

        const box = $('variant-groups');
        box.innerHTML = '';

        groups.forEach(group => {
            const isSingle = group.selectionMode === 'Single';
            const wrap = document.createElement('div');
            wrap.className = 'mb-3';

            const label = document.createElement('div');
            label.className = 'fw-semibold small mb-1';
            label.textContent = group.name + (group.isRequired ? ' *' : '');
            wrap.appendChild(label);

            group.options.forEach(option => {
                const id = 'opt-' + option.id;
                const row = document.createElement('div');
                row.className = 'form-check';
                row.innerHTML =
                    '<input class="form-check-input variant-input" ' +
                    'type="' + (isSingle ? 'radio' : 'checkbox') + '" ' +
                    'name="grp-' + group.id + '" id="' + id + '" value="' + option.id + '">' +
                    '<label class="form-check-label d-flex justify-content-between" for="' + id + '">' +
                    '<span></span><span class="text-muted small"></span></label>';
                row.querySelector('label span:first-child').textContent = option.name;
                row.querySelector('label span:last-child').textContent =
                    option.priceDelta > 0 ? '+' + rupiah(option.priceDelta) : '';
                row.querySelector('input').onchange = updateVariantState;
                wrap.appendChild(row);
            });

            box.appendChild(wrap);
        });

        updateVariantState();
        bootstrap.Modal.getOrCreateInstance($('variant-modal')).show();
    }

    function selectedVariantIds() {
        return Array.from(document.querySelectorAll('.variant-input:checked'))
            .map(i => parseInt(i.value, 10));
    }

    function updateVariantState() {
        const ids = selectedVariantIds();
        const groups = groupsFor(pendingProduct);

        let delta = 0;
        ids.forEach(id => {
            groups.forEach(g => {
                const o = g.options.find(x => x.id === id);
                if (o) delta += o.priceDelta;
            });
        });
        $('variant-price').textContent = rupiah(pendingProduct.basePrice + delta);

        // Grup wajib harus terisi sebelum tombol Tambah aktif. Server tetap
        // memeriksa ulang aturan yang sama saat checkout.
        const allRequiredChosen = groups
            .filter(g => g.isRequired)
            .every(g => g.options.some(o => ids.includes(o.id)));

        $('variant-add').disabled = !allRequiredChosen;
    }

    function onVariantAdd() {
        const ids = selectedVariantIds();
        const groups = groupsFor(pendingProduct);

        let label = [], delta = 0;
        groups.forEach(g => g.options.forEach(o => {
            if (ids.includes(o.id)) { label.push(o.name); delta += o.priceDelta; }
        }));

        addLine(pendingProduct, ids, $('variant-note').value.trim(), label.join(', '), delta);
        bootstrap.Modal.getOrCreateInstance($('variant-modal')).hide();
    }

    // ---- Keranjang ---------------------------------------------------------

    function addLine(product, variantOptionIds, note, variantLabel, variantDelta) {
        const candidate = { productId: product.id, variantOptionIds, note: note || '' };
        const existing = cart.lines.find(l => lineKey(l) === lineKey(candidate));

        if (existing) {
            existing.quantity += 1;
        } else {
            cart.lines.push({
                lineId: nextLineId++,
                productId: product.id,
                productName: product.name,     // tampilan saja
                basePrice: product.basePrice,  // tampilan saja
                variantOptionIds: variantOptionIds,
                variantLabel: variantLabel || '',
                variantDelta: variantDelta || 0,
                quantity: 1,
                note: note || ''
            });
        }
        renderCart();
    }

    function changeQty(lineId, delta) {
        const line = cart.lines.find(l => l.lineId === lineId);
        if (!line) return;
        line.quantity += delta;
        if (line.quantity <= 0) cart.lines = cart.lines.filter(l => l.lineId !== lineId);
        renderCart();
    }

    function renderCart() {
        const box = $('cart-lines');
        box.innerHTML = '';
        $('cart-empty').classList.toggle('d-none', cart.lines.length > 0);

        cart.lines.forEach(line => {
            const unit = line.basePrice + line.variantDelta;
            const row = document.createElement('div');
            row.className = 'cart-line border-bottom p-2';
            row.innerHTML =
                '<div class="d-flex justify-content-between">' +
                '  <div class="pe-2">' +
                '    <div class="fw-semibold small line-name"></div>' +
                '    <div class="text-muted line-variant" style="font-size:.75rem"></div>' +
                '    <div class="text-muted fst-italic line-note" style="font-size:.75rem"></div>' +
                '  </div>' +
                '  <div class="text-end text-nowrap">' +
                '    <div class="small line-total"></div>' +
                '    <div class="btn-group btn-group-sm mt-1">' +
                '      <button type="button" class="btn btn-outline-secondary btn-minus">-</button>' +
                '      <button type="button" class="btn btn-light disabled line-qty"></button>' +
                '      <button type="button" class="btn btn-outline-secondary btn-plus">+</button>' +
                '    </div>' +
                '  </div>' +
                '</div>';

            row.querySelector('.line-name').textContent = line.productName;
            row.querySelector('.line-variant').textContent = line.variantLabel;
            row.querySelector('.line-note').textContent = line.note;
            row.querySelector('.line-total').textContent = rupiah(unit * line.quantity);
            row.querySelector('.line-qty').textContent = line.quantity;
            row.querySelector('.btn-minus').onclick = () => changeQty(line.lineId, -1);
            row.querySelector('.btn-plus').onclick = () => changeQty(line.lineId, +1);

            box.appendChild(row);
        });

        renderTotals();
    }

    function computeTotals() {
        const round = n => Math.round(n);

        const subtotal = round(cart.lines.reduce(
            (sum, l) => sum + (l.basePrice + l.variantDelta) * l.quantity, 0));

        const discount = round(subtotal * cart.discountPercent / 100);
        const afterDiscount = subtotal - discount;
        const service = round(afterDiscount * catalog.serviceChargePercent / 100);
        const tax = round((afterDiscount + service) * catalog.taxPercent / 100);

        return { subtotal, discount, service, tax, total: afterDiscount + service + tax };
    }

    function renderTotals() {
        const t = computeTotals();

        $('sum-subtotal').textContent = rupiah(t.subtotal);
        $('sum-discount').textContent = '-' + rupiah(t.discount);
        $('row-discount').hidden = t.discount === 0;
        $('sum-service').textContent = rupiah(t.service);
        $('row-service').hidden = catalog.serviceChargePercent === 0;
        $('sum-tax').textContent = rupiah(t.tax);
        $('sum-total').textContent = rupiah(t.total);

        const isCash = cart.paymentMethod === 'Cash';
        $('cash-box').style.display = isCash ? '' : 'none';

        const paid = isCash ? cart.amountPaid : t.total;
        $('sum-change').textContent = rupiah(Math.max(0, paid - t.total));

        // Tombol bayar mati kalau keranjang kosong atau uang tunai belum cukup.
        $('btn-pay').disabled = cart.lines.length === 0 || (isCash && paid < t.total);
    }

    // ---- Checkout ----------------------------------------------------------

    function buildCheckoutPayload() {
        // Hanya id, jumlah, dan catatan. Tidak ada harga sama sekali.
        return {
            items: cart.lines.map(l => ({
                productId: l.productId,
                quantity: l.quantity,
                variantOptionIds: l.variantOptionIds,
                note: l.note || null
            })),
            discountPercent: cart.discountPercent,
            discountAmount: 0,
            paymentMethod: cart.paymentMethod,
            amountPaid: cart.paymentMethod === 'Cash' ? cart.amountPaid : computeTotals().total,
            note: null
        };
    }

    async function checkout() {
        const btn = $('btn-pay');
        btn.disabled = true;              // dimatikan SEBELUM await — membunuh double-submit
        showError(null);

        try {
            const res = await fetch('/api/pos/checkout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'RequestVerificationToken': antiforgeryToken()
                },
                body: JSON.stringify(buildCheckoutPayload())
            });

            if (res.status === 401) { location.href = '/Account/Login'; return; }

            if (!res.ok) {
                const err = await res.json().catch(() => ({ errors: ['Terjadi kesalahan tak terduga.'] }));
                showError((err.errors || ['Gagal menyimpan transaksi.']).join('\n'));
                renderTotals();           // aktifkan kembali tombolnya
                return;
            }

            const result = await res.json();
            showReceipt(result);
            clearCart();
        } catch (e) {
            showError('Tidak bisa menghubungi server. Coba lagi.');
            renderTotals();
        }
    }

    function showError(message) {
        const box = $('checkout-error');
        box.classList.toggle('d-none', !message);
        box.textContent = message || '';
    }

    function clearCart() {
        cart.lines = [];
        cart.discountPercent = 0;
        cart.amountPaid = 0;
        $('discount-percent').value = 0;
        $('amount-paid').value = 0;
        renderCart();
    }

    // ---- Struk -------------------------------------------------------------

    function showReceipt(result) {
        const r = result.receipt;
        const warnBox = $('receipt-warnings');
        warnBox.classList.toggle('d-none', !result.warnings || result.warnings.length === 0);
        warnBox.textContent = (result.warnings || []).join('\n');

        const line = (label, value, cls) =>
            '<div class="d-flex justify-content-between ' + (cls || '') + '">' +
            '<span>' + label + '</span><span>' + value + '</span></div>';

        let html = '<div id="receipt" class="receipt">';
        html += '<div class="text-center mb-2">' +
            '<div class="fw-bold">' + escapeHtml(r.cafeName) + '</div>' +
            '<div class="small">' + escapeHtml(r.cafeAddress) + '</div>' +
            '<div class="small">' + escapeHtml(r.cafePhone) + '</div>' +
            '</div><hr>';

        html += '<div class="small">' + escapeHtml(r.orderNumber) + '</div>';
        html += '<div class="small">' + new Date(r.orderDate).toLocaleString('id-ID') + '</div>';
        html += '<div class="small">Kasir: ' + escapeHtml(r.cashierName) + '</div><hr>';

        r.lines.forEach(l => {
            html += '<div class="small">' + escapeHtml(l.productName);
            if (l.variantDescription) html += ' <em>(' + escapeHtml(l.variantDescription) + ')</em>';
            html += '</div>';
            if (l.note) html += '<div class="small fst-italic">* ' + escapeHtml(l.note) + '</div>';
            html += line(l.quantity + ' x ' + rupiah(l.unitPrice), rupiah(l.lineTotal), 'small');
        });

        html += '<hr>';
        html += line('Subtotal', rupiah(r.subtotal), 'small');
        if (r.discountAmount > 0) html += line('Diskon', '-' + rupiah(r.discountAmount), 'small');
        if (r.serviceChargeAmount > 0) html += line('Service ' + r.serviceChargePercent + '%', rupiah(r.serviceChargeAmount), 'small');
        if (r.taxAmount > 0) html += line('Pajak ' + r.taxPercent + '%', rupiah(r.taxAmount), 'small');
        html += line('<strong>TOTAL</strong>', '<strong>' + rupiah(r.grandTotal) + '</strong>');
        html += line('Bayar (' + r.paymentMethod + ')', rupiah(r.amountPaid), 'small');
        html += '<div class="d-flex justify-content-between fs-4 fw-bold mt-2">' +
            '<span>KEMBALI</span><span>' + rupiah(r.changeAmount) + '</span></div>';
        html += '<div class="text-center small mt-3">Terima kasih!</div>';
        html += '</div>';

        $('receipt-area').innerHTML = html;
        bootstrap.Modal.getOrCreateInstance($('receipt-modal')).show();
    }

    function escapeHtml(s) {
        const d = document.createElement('div');
        d.textContent = s == null ? '' : s;
        return d.innerHTML;
    }

    // ---- Inisialisasi ------------------------------------------------------

    async function init() {
        catalog = await apiGet('/api/pos/catalog');

        $('lbl-tax-pct').textContent = '(' + catalog.taxPercent + '%)';
        $('lbl-service-pct').textContent = '(' + catalog.serviceChargePercent + '%)';

        renderCategories();
        renderProducts();
        renderCart();

        $('product-search').oninput = renderProducts;
        $('variant-add').onclick = onVariantAdd;
        $('btn-clear').onclick = clearCart;
        $('btn-pay').onclick = checkout;
        $('btn-print').onclick = () => window.print();
        $('btn-new').onclick = () => {
            bootstrap.Modal.getOrCreateInstance($('receipt-modal')).hide();
            $('product-search').focus();
        };

        $('discount-percent').oninput = e => {
            cart.discountPercent = Math.min(100, Math.max(0, parseFloat(e.target.value) || 0));
            renderTotals();
        };

        $('amount-paid').oninput = e => {
            cart.amountPaid = parseFloat(e.target.value) || 0;
            renderTotals();
        };

        document.querySelectorAll('#payment-methods input').forEach(input => {
            input.onchange = () => { cart.paymentMethod = input.value; renderTotals(); };
        });

        document.querySelectorAll('#quick-cash button').forEach(btn => {
            btn.onclick = () => {
                const v = btn.dataset.cash;
                cart.amountPaid = v === 'exact' ? computeTotals().total : parseInt(v, 10);
                $('amount-paid').value = cart.amountPaid;
                renderTotals();
            };
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        init().catch(e => console.error('Gagal memuat katalog:', e));
    });
})();
