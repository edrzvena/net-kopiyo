using KopiYo.Common;
using KopiYo.Data;
using KopiYo.DTOs.Reports;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Reports;

namespace KopiYo.Services;

public sealed class ReportService(AppDbContext db) : IReportService
{
    /// <summary>
    /// Rentang setengah terbuka: >= From dan &lt; To, dengan To = tanggal akhir + 1 hari.
    ///
    /// Ini bug laporan paling sering di dunia: memakai &lt;= tanggalAkhir diam-diam
    /// membuang SEMUA penjualan setelah jam 00:00:00.000 di hari terakhir — yaitu
    /// hampir seluruh penjualan hari itu. Dibuat sekali di sini, benar selamanya.
    /// </summary>
    private static (DateTime From, DateTime To) Range(DateOnly from, DateOnly to)
        => (from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));

    /// <summary>
    /// Filter dasar semua laporan: HANYA order berstatus Paid.
    /// Order yang di-void atau di-refund tetap ada di tabel selamanya (menghapusnya
    /// akan menghancurkan jejak audit), tapi tidak pernah dihitung sebagai omzet.
    /// </summary>
    private IQueryable<Order> PaidOrders(DateTime from, DateTime to)
        => db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid && o.OrderDate >= from && o.OrderDate < to);

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (f, t) = Range(from, to);
        var query = PaidOrders(f, t);

        // Cast ke (decimal?) itu WAJIB: SUM() atas nol baris mengembalikan NULL di SQL,
        // dan memetakan NULL ke decimal non-nullable melempar InvalidOperationException.
        // Tanpa ini, dashboard akan error 500 di pagi pertama yang belum ada penjualan.
        var revenue = await query.SumAsync(o => (decimal?)o.GrandTotal, ct) ?? 0m;
        var discount = await query.SumAsync(o => (decimal?)o.DiscountAmount, ct) ?? 0m;
        var tax = await query.SumAsync(o => (decimal?)o.TaxAmount, ct) ?? 0m;
        var orderCount = await query.CountAsync(ct);

        var itemCount = await db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.Status == OrderStatus.Paid
                         && oi.Order.OrderDate >= f && oi.Order.OrderDate < t)
            .SumAsync(oi => (int?)oi.Quantity, ct) ?? 0;

        var average = orderCount == 0 ? 0m : Math.Round(revenue / orderCount, 0);

        return new SalesSummaryDto(revenue, orderCount, itemCount, average, discount, tax);
    }

    public async Task<IReadOnlyList<PaymentBreakdownDto>> GetPaymentBreakdownAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (f, t) = Range(from, to);

        // Diproyeksikan ke tipe anonim dulu, baru dipetakan ke DTO setelah
        // ToListAsync. EF Core 10 tidak bisa menerjemahkan GroupBy yang langsung
        // memanggil constructor record positional, dan .ToString() pada enum
        // ber-value-converter juga tidak punya padanan SQL.
        var rows = await PaidOrders(f, t)
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new { Method = g.Key, Count = g.Count(), Total = g.Sum(x => x.GrandTotal) })
            .ToListAsync(ct);

        return rows
            .Select(r => new PaymentBreakdownDto(r.Method.ToString(), r.Count, r.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<DailySalesPointDto>> GetDailySeriesAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (f, t) = Range(from, to);

        var raw = await PaidOrders(f, t)
            .GroupBy(o => o.OrderDate.Date)          // diterjemahkan jadi CAST(OrderDate AS date)
            .Select(g => new { Day = g.Key, Revenue = g.Sum(x => x.GrandTotal), Count = g.Count() })
            .ToListAsync(ct);

        // Hari kosong diisi SETELAH data ter-materialisasi. SQL tidak bisa membuat
        // kalender tanpa tabel angka, dan LEFT JOIN ke tabel tanggal buatan adalah
        // alternatif "pintar" yang tidak akan dipahami siapa pun tiga bulan lagi.
        var byDay = raw.ToDictionary(x => DateOnly.FromDateTime(x.Day));
        var result = new List<DailySalesPointDto>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            result.Add(byDay.TryGetValue(day, out var hit)
                ? new DailySalesPointDto(day, hit.Revenue, hit.Count)
                : new DailySalesPointDto(day, 0m, 0));
        }

        return result;
    }

    public async Task<IReadOnlyList<BestSellerDto>> GetBestSellersAsync(
        DateOnly from, DateOnly to, int top, CancellationToken ct)
    {
        var (f, t) = Range(from, to);

        // Dikelompokkan ke ProductNameSnapshot, BUKAN join ke tabel Products.
        // Inilah imbalan desain snapshot: laporan tetap benar walau produknya
        // sudah di-rename atau dinonaktifkan.
        //
        // Konsekuensinya: produk yang pernah diganti nama akan muncul sebagai dua
        // baris terpisah. Itu memang BENAR secara historis — nama itulah yang
        // tercetak di struk saat itu.
        var rows = await db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.Status == OrderStatus.Paid
                         && oi.Order.OrderDate >= f && oi.Order.OrderDate < t)
            .GroupBy(oi => new { oi.ProductId, oi.ProductNameSnapshot })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductNameSnapshot,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(top)
            .ToListAsync(ct);

        return rows
            .Select(r => new BestSellerDto(r.ProductId, r.ProductNameSnapshot, r.Quantity, r.Revenue))
            .ToList();
    }

    public async Task<IReadOnlyList<CashierSalesDto>> GetSalesByCashierAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (f, t) = Range(from, to);

        var rows = await PaidOrders(f, t)
            .GroupBy(o => new { o.CashierId, o.CashierNameSnapshot })
            .Select(g => new
            {
                g.Key.CashierId,
                g.Key.CashierNameSnapshot,
                OrderCount = g.Count(),
                Revenue = g.Sum(x => x.GrandTotal),
                Discount = g.Sum(x => x.DiscountAmount)
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new CashierSalesDto(
                r.CashierId, r.CashierNameSnapshot, r.OrderCount, r.Revenue, r.Discount,
                r.OrderCount == 0 ? 0m : Math.Round(r.Revenue / r.OrderCount, 0)))
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    public async Task<DashboardViewModel> GetDashboardAsync(DateOnly day, CancellationToken ct)
    {
        var weekStart = day.AddDays(-6);

        return new DashboardViewModel
        {
            Day = day,
            Summary = await GetSalesSummaryAsync(day, day, ct),
            PaymentBreakdown = await GetPaymentBreakdownAsync(day, day, ct),
            BestSellers = await GetBestSellersAsync(day, day, 5, ct),
            LastSevenDays = await GetDailySeriesAsync(weekStart, day, ct),
            LowStockCount = await db.Ingredients.AsNoTracking()
                .CountAsync(i => i.IsActive && i.StockQty <= i.MinStockQty, ct)
        };
    }

    public async Task<IReadOnlyList<OrderCsvRowDto>> GetOrdersForExportAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (f, t) = Range(from, to);

        // Export sengaja menyertakan SEMUA status, termasuk yang di-void/refund,
        // karena file ini dipakai untuk rekonsiliasi — akuntan perlu melihat
        // pembatalannya, bukan hanya yang berhasil.
        var rows = await db.Orders.AsNoTracking()
            .Where(o => o.OrderDate >= f && o.OrderDate < t)
            .OrderBy(o => o.OrderDate)
            .Select(o => new
            {
                o.OrderNumber, o.OrderDate, o.CashierNameSnapshot, o.PaymentMethod,
                o.Subtotal, o.DiscountAmount, o.ServiceChargeAmount, o.TaxAmount,
                o.GrandTotal, o.Status
            })
            .ToListAsync(ct);

        // .ToString() pada enum dipanggil di memori, bukan di dalam query.
        return rows
            .Select(o => new OrderCsvRowDto(
                o.OrderNumber, o.OrderDate, o.CashierNameSnapshot, o.PaymentMethod.ToString(),
                o.Subtotal, o.DiscountAmount, o.ServiceChargeAmount, o.TaxAmount,
                o.GrandTotal, o.Status.ToString()))
            .ToList();
    }
}
