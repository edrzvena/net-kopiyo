using KopiYo.Common;

namespace KopiYo.Models;

/// <summary>
/// ATURAN INTI: Order adalah dokumen keuangan yang immutable.
/// Dia TIDAK PERNAH join ke tabel master untuk menampilkan dirinya sendiri.
/// Tabel master (Product, User, Category) = kebenaran SEKARANG.
/// Tabel order = kebenaran SAAT ITU.
///
/// Karena itu semua yang dicetak di struk dan dipakai laporan disimpan sebagai
/// snapshot di sini: nama kasir, nama produk, harga satuan, bahkan persentase
/// pajak dan service charge. Kalau harga Latte naik besok atau PB1 berubah dari
/// 10% ke 11%, struk kemarin dan laporan kemarin TIDAK BOLEH ikut berubah.
/// </summary>
public class Order
{
    public int Id { get; set; }

    /// <summary>Unik. Format: KY-20260808-0001, urutannya reset tiap hari.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Waktu WIB, bukan UTC. Di-index bareng Status karena dipakai semua laporan.</summary>
    public DateTime OrderDate { get; set; }

    public int CashierId { get; set; }
    public User Cashier { get; set; } = null!;

    /// <summary>Snapshot nama kasir — tetap benar walau user-nya di-rename atau dinonaktifkan.</summary>
    public string CashierNameSnapshot { get; set; } = string.Empty;

    // ---- Uang. Semua sudah dibulatkan ke rupiah utuh saat disimpan, sehingga
    //      Subtotal - DiscountAmount + ServiceChargeAmount + TaxAmount == GrandTotal persis.
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }

    /// <summary>Snapshot persentase yang berlaku saat penjualan, bukan yang berlaku sekarang.</summary>
    public decimal ServiceChargePercent { get; set; }
    public decimal ServiceChargeAmount { get; set; }

    /// <summary>Snapshot persentase pajak saat penjualan.</summary>
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public decimal AmountPaid { get; set; }

    /// <summary>Kembalian. Selalu 0 untuk QRIS/Debit.</summary>
    public decimal ChangeAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Paid;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    // ---- Pembalikan (void / refund). Field-nya sengaja generik "Reversed",
    //      dipakai bersama oleh void dan refund karena datanya identik.
    public DateTime? ReversedAt { get; set; }
    public int? ReversedByUserId { get; set; }
    public User? ReversedByUser { get; set; }
    public string? ReversalReason { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
