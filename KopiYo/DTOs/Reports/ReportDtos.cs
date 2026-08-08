namespace KopiYo.DTOs.Reports;

public sealed record SalesSummaryDto(
    decimal Revenue,
    int OrderCount,
    int ItemCount,
    decimal AverageOrderValue,
    decimal TotalDiscount,
    decimal TotalTax);

public sealed record PaymentBreakdownDto(string PaymentMethod, int OrderCount, decimal Total);

public sealed record BestSellerDto(int ProductId, string ProductName, int QuantitySold, decimal Revenue);

public sealed record CashierSalesDto(
    int CashierId,
    string CashierName,
    int OrderCount,
    decimal Revenue,
    decimal TotalDiscount,
    decimal AverageOrderValue);

public sealed record DailySalesPointDto(DateOnly Day, decimal Revenue, int OrderCount);

/// <summary>Satu baris di file CSV penjualan.</summary>
public sealed record OrderCsvRowDto(
    string OrderNumber,
    DateTime OrderDate,
    string CashierName,
    string PaymentMethod,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal ServiceChargeAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    string Status);
