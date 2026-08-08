using KopiYo.DTOs.Reports;

namespace KopiYo.ViewModels.Reports;

public class DashboardViewModel
{
    public DateOnly Day { get; set; }
    public SalesSummaryDto Summary { get; set; } = new(0, 0, 0, 0, 0, 0);
    public IReadOnlyList<PaymentBreakdownDto> PaymentBreakdown { get; set; } = [];
    public IReadOnlyList<BestSellerDto> BestSellers { get; set; } = [];
    public IReadOnlyList<DailySalesPointDto> LastSevenDays { get; set; } = [];
    public int LowStockCount { get; set; }
}

/// <summary>Dipakai bersama oleh laporan penjualan, per kasir, dan menu terlaris.</summary>
public class DateRangeViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public class SalesReportViewModel : DateRangeViewModel
{
    public SalesSummaryDto Summary { get; set; } = new(0, 0, 0, 0, 0, 0);
    public IReadOnlyList<DailySalesPointDto> Days { get; set; } = [];
    public IReadOnlyList<PaymentBreakdownDto> PaymentBreakdown { get; set; } = [];
}

public class CashierReportViewModel : DateRangeViewModel
{
    public IReadOnlyList<CashierSalesDto> Rows { get; set; } = [];
}

public class BestSellersViewModel : DateRangeViewModel
{
    public IReadOnlyList<BestSellerDto> Rows { get; set; } = [];
}
