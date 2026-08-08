using System.Globalization;
using KopiYo.Common;
using KopiYo.DTOs.Reports;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

/// <summary>
/// Role ada di level CLASS. Inilah batas keamanan yang sebenarnya — menyembunyikan
/// link di navbar hanya kosmetik. Kasir yang mengetik /Reports/Dashboard langsung
/// di address bar akan dilempar ke AccessDenied oleh middleware, sebelum action ini jalan.
/// </summary>
[Authorize(Roles = AppConstants.Roles.Admin)]
public class ReportsController(
    IReportService reports,
    ICsvExporter csv,
    IDateTimeProvider clock) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Dashboard(DateOnly? date, CancellationToken ct)
        => View(await reports.GetDashboardAsync(date ?? clock.TodayWib, ct));

    [HttpGet]
    public async Task<IActionResult> Sales(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);

        return View(new SalesReportViewModel
        {
            From = f,
            To = t,
            Summary = await reports.GetSalesSummaryAsync(f, t, ct),
            Days = await reports.GetDailySeriesAsync(f, t, ct),
            PaymentBreakdown = await reports.GetPaymentBreakdownAsync(f, t, ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> ByCashier(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        return View(new CashierReportViewModel
        {
            From = f, To = t,
            Rows = await reports.GetSalesByCashierAsync(f, t, ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> BestSellers(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        return View(new BestSellersViewModel
        {
            From = f, To = t,
            Rows = await reports.GetBestSellersAsync(f, t, 20, ct)
        });
    }

    /// <summary>
    /// Link export cukup &lt;a href&gt; GET biasa — cookie autentikasi ikut otomatis,
    /// tidak perlu JS, blob, atau fetch.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportSalesCsv(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        var rows = await reports.GetOrdersForExportAsync(f, t, ct);

        var bytes = csv.Export(rows, [
            new CsvColumn<OrderCsvRowDto>("No. Order", r => r.OrderNumber),
            new CsvColumn<OrderCsvRowDto>("Tanggal", r => r.OrderDate.ToString("yyyy-MM-dd HH:mm:ss")),
            new CsvColumn<OrderCsvRowDto>("Kasir", r => r.CashierName),
            new CsvColumn<OrderCsvRowDto>("Metode Bayar", r => r.PaymentMethod),
            // InvariantCulture tanpa pemisah ribuan. JANGAN pakai "N0" — Excel akan
            // membaca "91.300" sebagai sembilan puluh satu koma tiga.
            new CsvColumn<OrderCsvRowDto>("Subtotal", r => Num(r.Subtotal)),
            new CsvColumn<OrderCsvRowDto>("Diskon", r => Num(r.DiscountAmount)),
            new CsvColumn<OrderCsvRowDto>("Service", r => Num(r.ServiceChargeAmount)),
            new CsvColumn<OrderCsvRowDto>("Pajak", r => Num(r.TaxAmount)),
            new CsvColumn<OrderCsvRowDto>("Total", r => Num(r.GrandTotal)),
            new CsvColumn<OrderCsvRowDto>("Status", r => r.Status)
        ]);

        return File(bytes, "text/csv", $"penjualan-{f:yyyyMMdd}-{t:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportBestSellersCsv(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        var rows = await reports.GetBestSellersAsync(f, t, 500, ct);

        var bytes = csv.Export(rows, [
            new CsvColumn<BestSellerDto>("Produk", r => r.ProductName),
            new CsvColumn<BestSellerDto>("Terjual", r => r.QuantitySold.ToString(CultureInfo.InvariantCulture)),
            new CsvColumn<BestSellerDto>("Omzet", r => Num(r.Revenue))
        ]);

        return File(bytes, "text/csv", $"menu-terlaris-{f:yyyyMMdd}-{t:yyyyMMdd}.csv");
    }

    private static string Num(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Default rentang: 7 hari terakhir sampai hari ini (WIB).</summary>
    private (DateOnly From, DateOnly To) DefaultRange(DateOnly? from, DateOnly? to)
    {
        var t = to ?? clock.TodayWib;
        var f = from ?? t.AddDays(-6);
        return f > t ? (t, t) : (f, t);
    }
}
