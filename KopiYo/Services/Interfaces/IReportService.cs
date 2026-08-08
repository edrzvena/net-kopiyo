using KopiYo.DTOs.Reports;
using KopiYo.ViewModels.Reports;

namespace KopiYo.Services.Interfaces;

/// <summary>Definisi satu kolom CSV: judulnya dan cara mengambil nilainya dari satu baris.</summary>
public sealed record CsvColumn<T>(string Header, Func<T, string?> Value);

public interface ICsvExporter
{
    byte[] Export<T>(IEnumerable<T> rows, IReadOnlyList<CsvColumn<T>> columns);
}

public interface IReportService
{
    Task<DashboardViewModel> GetDashboardAsync(DateOnly day, CancellationToken ct);

    Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<PaymentBreakdownDto>> GetPaymentBreakdownAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<DailySalesPointDto>> GetDailySeriesAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<BestSellerDto>> GetBestSellersAsync(DateOnly from, DateOnly to, int top, CancellationToken ct);
    Task<IReadOnlyList<CashierSalesDto>> GetSalesByCashierAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<OrderCsvRowDto>> GetOrdersForExportAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
