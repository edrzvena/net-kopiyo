using System.Data;
using KopiYo.Common;
using KopiYo.Data;
using KopiYo.DTOs.Pos;
using KopiYo.Mappings;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Orders;
using KopiYo.ViewModels.Shared;
using Microsoft.Extensions.Options;

namespace KopiYo.Services;

public sealed class OrderService(
    AppDbContext db,
    IOrderNumberGenerator numbers,
    IInventoryService inventory,
    IDateTimeProvider clock,
    IOptions<KopiYoSettings> settings,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly KopiYoSettings _settings = settings.Value;

    public async Task<ServiceResult<OrderResultDto>> CreateOrderAsync(
        CreateOrderDto dto, int cashierId, CancellationToken ct)
    {
        // ---- 0. Validasi bentuk. Murah, dan dikerjakan sebelum menyentuh database.
        if (dto.Items.Count == 0)
            return ServiceResult<OrderResultDto>.Fail("Keranjang kosong.");
        if (dto.Items.Any(i => i.Quantity is < 1 or > 999))
            return ServiceResult<OrderResultDto>.Fail("Jumlah item tidak valid.");
        if (dto.DiscountPercent is < 0 or > 100)
            return ServiceResult<OrderResultDto>.Fail("Diskon persen tidak valid.");

        // EnableRetryOnFailure aktif di Program.cs. Begitu itu menyala, memanggil
        // BeginTransaction manual di luar execution strategy akan melempar
        // "The configured execution strategy does not support user-initiated transactions".
        // Delegate di bawah aman diulang karena membaca ulang semuanya dari awal.
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            // ---- 1. Ambil master data dalam DUA query, tracked.
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var optionIds = dto.Items.SelectMany(i => i.VariantOptionIds).Distinct().ToList();

            var products = await db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariantGroups).ThenInclude(pvg => pvg.VariantGroup)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            var options = optionIds.Count == 0
                ? new Dictionary<int, VariantOption>()
                : await db.VariantOptions
                    .Include(o => o.VariantGroup)
                    .Where(o => optionIds.Contains(o.Id))
                    .ToDictionaryAsync(o => o.Id, ct);

            // ---- 2. Validasi keberadaan dan status aktif.
            foreach (var id in productIds)
            {
                if (!products.TryGetValue(id, out var p))
                    return ServiceResult<OrderResultDto>.Fail($"Produk #{id} tidak ditemukan.");
                if (!p.IsActive)
                    return ServiceResult<OrderResultDto>.Fail($"Produk '{p.Name}' sudah tidak aktif.");
            }

            foreach (var id in optionIds)
            {
                if (!options.TryGetValue(id, out var o))
                    return ServiceResult<OrderResultDto>.Fail($"Varian #{id} tidak ditemukan.");
                if (!o.IsActive)
                    return ServiceResult<OrderResultDto>.Fail($"Varian '{o.Name}' sudah tidak aktif.");
            }

            var now = clock.NowWib;
            var order = new Order();
            decimal subtotal = 0m;

            // ---- 3. Per baris: validasi varian, lalu hitung harganya.
            foreach (var line in dto.Items)
            {
                var product = products[line.ProductId];
                var chosen = line.VariantOptionIds.Select(id => options[id]).ToList();

                var lineError = ValidateVariants(product, chosen);
                if (lineError is not null)
                    return ServiceResult<OrderResultDto>.Fail(lineError);

                // ---- HARGA: sepenuhnya dari database. Apa pun yang dikirim client
                //      soal harga tidak pernah dibaca — DTO-nya bahkan tidak punya
                //      field-nya. Inilah alasan manipulasi harga dari DevTools sia-sia.
                var delta = chosen.Sum(o => o.PriceDelta);
                var unitPrice = product.BasePrice + delta;
                var lineTotal = unitPrice * line.Quantity;
                subtotal += lineTotal;

                var item = new OrderItem
                {
                    ProductId = product.Id,
                    ProductNameSnapshot = product.Name,
                    CategoryNameSnapshot = product.Category.Name,
                    UnitBasePrice = product.BasePrice,
                    VariantDeltaTotal = delta,
                    UnitPrice = unitPrice,
                    Quantity = line.Quantity,
                    LineTotal = lineTotal,
                    VariantDescription = string.Join(", ", chosen
                        .OrderBy(o => o.VariantGroup.DisplayOrder)
                        .ThenBy(o => o.DisplayOrder)
                        .Select(o => o.Name)),
                    Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim()
                };

                foreach (var option in chosen)
                {
                    item.Variants.Add(new OrderItemVariant
                    {
                        VariantOptionId = option.Id,
                        GroupNameSnapshot = option.VariantGroup.Name,
                        OptionNameSnapshot = option.Name,
                        PriceDelta = option.PriceDelta
                    });
                }

                order.Items.Add(item);
            }

            // ---- 4. Total order. Dibulatkan ke rupiah utuh di SETIAP langkah dan
            //      nilai bulatnya yang disimpan, sehingga
            //      Subtotal - Diskon + Service + Pajak == GrandTotal persis.
            //      Kalau tidak, angka di struk bisa meleset 1 rupiah dari total tersimpan.
            order.Subtotal = subtotal.RoundRupiah();

            order.DiscountPercent = dto.DiscountPercent;
            order.DiscountAmount = dto.DiscountPercent > 0
                ? (order.Subtotal * dto.DiscountPercent / 100m).RoundRupiah()
                : dto.DiscountAmount.RoundRupiah();

            if (order.DiscountAmount > order.Subtotal)
                return ServiceResult<OrderResultDto>.Fail("Diskon melebihi subtotal.");

            var afterDiscount = order.Subtotal - order.DiscountAmount;

            order.ServiceChargePercent = _settings.ServiceChargePercent;
            order.ServiceChargeAmount = (afterDiscount * _settings.ServiceChargePercent / 100m).RoundRupiah();

            order.TaxPercent = _settings.TaxPercent;
            order.TaxAmount = ((afterDiscount + order.ServiceChargeAmount) * _settings.TaxPercent / 100m)
                .RoundRupiah();

            order.GrandTotal = afterDiscount + order.ServiceChargeAmount + order.TaxAmount;

            // ---- 5. Pembayaran.
            order.PaymentMethod = dto.PaymentMethod;
            if (dto.PaymentMethod == PaymentMethod.Cash)
            {
                if (dto.AmountPaid < order.GrandTotal)
                    return ServiceResult<OrderResultDto>.Fail(
                        $"Uang dibayar ({dto.AmountPaid.ToRupiah()}) kurang dari total ({order.GrandTotal.ToRupiah()}).");

                order.AmountPaid = dto.AmountPaid;
                order.ChangeAmount = dto.AmountPaid - order.GrandTotal;
            }
            else
            {
                // QRIS dan debit selalu pas — tidak ada kembalian.
                order.AmountPaid = order.GrandTotal;
                order.ChangeAmount = 0m;
            }

            // ---- 6. Snapshot kasir.
            var cashier = await db.Users.FirstOrDefaultAsync(u => u.Id == cashierId, ct);
            if (cashier is null)
                return ServiceResult<OrderResultDto>.Fail("Data kasir tidak ditemukan.", ErrorKind.NotFound);

            order.CashierId = cashier.Id;
            order.CashierNameSnapshot = cashier.FullName;
            order.OrderDate = now;
            order.CreatedAt = now;
            order.Status = OrderStatus.Paid;
            order.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

            // ---- 7. Nomor order — di dalam transaksi, dengan row lock.
            order.OrderNumber = await numbers.NextAsync(DateOnly.FromDateTime(now), ct);

            // ---- 8. Stok bahan: hitung kebutuhan, cek kecukupan, potong, catat ledger.
            //      Semua ini masih di dalam transaksi yang sama, jadi kalau stok kurang
            //      seluruhnya di-rollback: tidak ada order, stok tidak berubah, dan
            //      nomor order tidak terbuang.
            var consumption = await inventory.BuildConsumptionAsync(
                dto.Items.Select(i => (i.ProductId, i.Quantity)).ToList(), ct);

            var consumeResult = await inventory.ConsumeForOrderAsync(order, consumption, cashierId, ct);
            if (!consumeResult.Succeeded)
                return ServiceResult<OrderResultDto>.From(consumeResult);

            var warnings = consumeResult.Value?.ToList() ?? [];

            // ---- 9. SATU SaveChanges untuk order + item + varian + stok + movement.
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogInformation(
                "Order {OrderNumber} dibuat oleh {Cashier}, total {Total}.",
                order.OrderNumber, order.CashierNameSnapshot, order.GrandTotal);

            return ServiceResult<OrderResultDto>.Ok(
                new OrderResultDto(order.ToReceiptDto(_settings), warnings));

            // Catatan: return lebih awal dari dalam delegate ini membuat `await using var tx`
            // ter-dispose tanpa commit, yang otomatis me-rollback. Tidak perlu
            // RollbackAsync eksplisit di setiap cabang gagal.
        });
    }

    /// <summary>
    /// Tiga aturan varian, semuanya diperiksa ulang di server meskipun UI sudah
    /// mencegahnya: grup harus terpasang di produk itu, grup Single maksimal satu
    /// pilihan, grup wajib harus terisi.
    /// </summary>
    private static string? ValidateVariants(Product product, List<VariantOption> chosen)
    {
        var attachedGroups = product.ProductVariantGroups
            .Select(g => g.VariantGroup)
            .ToDictionary(g => g.Id);

        foreach (var option in chosen)
        {
            if (!attachedGroups.ContainsKey(option.VariantGroupId))
                return $"Varian '{option.Name}' tidak berlaku untuk produk '{product.Name}'.";
        }

        foreach (var group in chosen.GroupBy(o => o.VariantGroupId))
        {
            var meta = attachedGroups[group.Key];
            if (meta.SelectionMode == VariantSelectionMode.Single && group.Count() > 1)
                return $"Pilih hanya satu {meta.Name} untuk '{product.Name}'.";
        }

        foreach (var group in attachedGroups.Values.Where(g => g.IsRequired && g.IsActive))
        {
            if (chosen.All(o => o.VariantGroupId != group.Id))
                return $"'{product.Name}' wajib memilih {group.Name}.";
        }

        return null;
    }

    public async Task<ReceiptDto?> GetReceiptAsync(int orderId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order?.ToReceiptDto(_settings);
    }

    public async Task<int?> GetCashierIdAsync(int orderId, CancellationToken ct)
        => await db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => (int?)o.CashierId)
            .FirstOrDefaultAsync(ct);

    // ---- Riwayat, void, dan refund ----------------------------------------

    public async Task<PagedList<OrderListItemViewModel>> GetPagedAsync(
        DateOnly? from, DateOnly? to, int? cashierId, OrderStatus? status, string? search,
        int page, int pageSize, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking();

        if (from.HasValue)
            query = query.Where(o => o.OrderDate >= from.Value.ToDateTime(TimeOnly.MinValue));

        // Setengah terbuka: < (tanggalAkhir + 1 hari). Memakai <= akan membuang
        // semua penjualan setelah jam 00:00:00.000 di hari terakhir.
        if (to.HasValue)
            query = query.Where(o => o.OrderDate < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

        if (cashierId is > 0) query = query.Where(o => o.CashierId == cashierId);
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => o.OrderNumber.Contains(term)
                                     || o.CashierNameSnapshot.Contains(term));
        }

        var projected = query
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Select(o => new OrderListItemViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                CashierName = o.CashierNameSnapshot,
                ItemCount = o.Items.Sum(i => (int?)i.Quantity) ?? 0,
                GrandTotal = o.GrandTotal,
                PaymentMethod = o.PaymentMethod,
                Status = o.Status
            });

        return await PagedList<OrderListItemViewModel>.CreateAsync(projected, page, pageSize, ct);
    }

    public async Task<OrderDetailsViewModel?> GetDetailsAsync(int orderId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.ReversedByUser)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null) return null;

        var movements = await db.StockMovements.AsNoTracking()
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.Id)
            .Select(m => new OrderStockMovementViewModel
            {
                IngredientName = m.Ingredient.Name,
                UnitLabel = m.Ingredient.Unit.ToString(),
                MovementType = m.MovementType,
                Quantity = m.Quantity,
                StockBefore = m.StockBefore,
                StockAfter = m.StockAfter
            })
            .ToListAsync(ct);

        var isToday = DateOnly.FromDateTime(order.OrderDate) == clock.TodayWib;

        return new OrderDetailsViewModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            CashierName = order.CashierNameSnapshot,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            ServiceChargePercent = order.ServiceChargePercent,
            ServiceChargeAmount = order.ServiceChargeAmount,
            TaxPercent = order.TaxPercent,
            TaxAmount = order.TaxAmount,
            GrandTotal = order.GrandTotal,
            AmountPaid = order.AmountPaid,
            ChangeAmount = order.ChangeAmount,
            Note = order.Note,
            ReversedAt = order.ReversedAt,
            ReversedByName = order.ReversedByUser?.FullName,
            ReversalReason = order.ReversalReason,
            Lines = order.Items.OrderBy(i => i.Id).Select(i => new OrderDetailLineViewModel
            {
                ProductName = i.ProductNameSnapshot,
                CategoryName = i.CategoryNameSnapshot,
                VariantDescription = i.VariantDescription,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
                Note = i.Note
            }).ToList(),
            StockMovements = movements,

            // Void hanya untuk order Paid di hari yang sama. Kesalahan hari kemarin
            // bukan "batal", melainkan pengembalian uang — dan itu refund.
            CanVoid = order.Status == OrderStatus.Paid && isToday,
            CanRefund = order.Status == OrderStatus.Paid
        };
    }

    public async Task<ReverseOrderViewModel?> GetForReverseAsync(
        int orderId, bool isVoid, CancellationToken ct)
        => await db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new ReverseOrderViewModel
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                GrandTotal = o.GrandTotal,
                IsVoid = isVoid,
                // Void: minumannya belum sempat dibuat, jadi bahan dikembalikan.
                // Refund: minumannya biasanya sudah jadi dan dibuang, jadi default-nya tidak.
                RestoreStock = isVoid
            })
            .FirstOrDefaultAsync(ct);

    public async Task<ServiceResult> ReverseOrderAsync(
        int orderId, bool isVoid, string reason, bool restoreStock, int adminUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ServiceResult.Fail("Alasan wajib diisi.");

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is null) return ServiceResult.Fail("Order tidak ditemukan.", ErrorKind.NotFound);

            // Penjaga idempotensi. Tanpa ini, mengirim ulang form void (refresh
            // halaman, klik dobel) akan mengembalikan stok DUA KALI.
            if (order.Status != OrderStatus.Paid)
                return ServiceResult.Fail(
                    order.Status == OrderStatus.Voided
                        ? "Order ini sudah dibatalkan."
                        : "Order ini sudah di-refund.");

            if (isVoid && DateOnly.FromDateTime(order.OrderDate) != clock.TodayWib)
                return ServiceResult.Fail(
                    "Void hanya untuk transaksi hari ini. Untuk transaksi lama, gunakan Refund.");

            if (restoreStock)
            {
                var restore = await inventory.RestoreForOrderAsync(
                    order, adminUserId,
                    $"{(isVoid ? "Void" : "Refund")} {order.OrderNumber}", ct);

                if (!restore.Succeeded) return restore;
            }

            order.Status = isVoid ? OrderStatus.Voided : OrderStatus.Refunded;
            order.ReversedAt = clock.NowWib;
            order.ReversedByUserId = adminUserId;
            order.ReversalReason = reason.Trim();

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogWarning(
                "Order {OrderNumber} di-{Action} oleh user #{UserId}. Alasan: {Reason}",
                order.OrderNumber, isVoid ? "void" : "refund", adminUserId, order.ReversalReason);

            return ServiceResult.Ok();
        });
    }
}
