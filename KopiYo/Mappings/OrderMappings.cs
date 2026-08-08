using KopiYo.Common;
using KopiYo.DTOs.Pos;
using KopiYo.Models;

namespace KopiYo.Mappings;

public static class OrderMappings
{
    /// <summary>
    /// Membangun struk dari entity Order yang SUDAH ter-materialisasi
    /// (lengkap dengan Items). Aman dipanggil di sini karena datanya sudah
    /// di memori — jangan pernah memanggilnya di dalam .Select() sebuah query EF.
    ///
    /// Perhatikan bahwa tidak ada satu pun nilai di sini yang diambil dari tabel
    /// master: semuanya kolom snapshot milik order itu sendiri.
    /// </summary>
    public static ReceiptDto ToReceiptDto(this Order order, KopiYoSettings settings) => new(
        order.Id,
        order.OrderNumber,
        order.OrderDate,
        order.CashierNameSnapshot,
        settings.CafeName,
        settings.CafeAddress,
        settings.CafePhone,
        order.Items
            .OrderBy(i => i.Id)
            .Select(i => new ReceiptLineDto(
                i.ProductNameSnapshot,
                i.VariantDescription,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal,
                i.Note))
            .ToList(),
        order.Subtotal,
        order.DiscountPercent,
        order.DiscountAmount,
        order.ServiceChargePercent,
        order.ServiceChargeAmount,
        order.TaxPercent,
        order.TaxAmount,
        order.GrandTotal,
        order.PaymentMethod,
        order.AmountPaid,
        order.ChangeAmount,
        order.Status,
        order.Note);
}
