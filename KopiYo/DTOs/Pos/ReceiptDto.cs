using KopiYo.Common;

namespace KopiYo.DTOs.Pos;

/// <summary>
/// Isi struk. Ini juga yang dikembalikan endpoint checkout, sehingga satu
/// checkout = satu round-trip, bukan "simpan lalu ambil struknya".
///
/// Semua nilai di sini berasal dari kolom snapshot di tabel Order/OrderItem,
/// bukan dari tabel master — itulah sebabnya mencetak ulang struk enam bulan
/// kemudian tetap menghasilkan angka yang sama persis.
/// </summary>
public sealed record ReceiptDto(
    int OrderId,
    string OrderNumber,
    DateTime OrderDate,
    string CashierName,
    string CafeName,
    string CafeAddress,
    string CafePhone,
    IReadOnlyList<ReceiptLineDto> Lines,
    decimal Subtotal,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal ServiceChargePercent,
    decimal ServiceChargeAmount,
    decimal TaxPercent,
    decimal TaxAmount,
    decimal GrandTotal,
    PaymentMethod PaymentMethod,
    decimal AmountPaid,
    decimal ChangeAmount,
    OrderStatus Status,
    string? Note);

public sealed record ReceiptLineDto(
    string ProductName,
    string VariantDescription,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Note);

/// <summary>
/// Hasil checkout: struknya plus daftar peringatan.
/// Warnings terisi kalau BlockSaleOnInsufficientStock = false dan penjualan
/// tetap diteruskan meskipun stok bahan kurang.
/// </summary>
public sealed record OrderResultDto(ReceiptDto Receipt, IReadOnlyList<string> Warnings);

/// <summary>Bentuk seragam untuk semua respons error API, supaya JS cukup menangani satu bentuk.</summary>
public sealed record ApiErrorDto(IReadOnlyList<string> Errors);
