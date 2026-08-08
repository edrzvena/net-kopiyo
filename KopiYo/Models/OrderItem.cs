namespace KopiYo.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Hanya untuk mengelompokkan laporan (best seller) dengan id yang stabil.
    /// Bukan sumber nama/harga — itu ada di kolom snapshot di bawah.
    /// FK-nya Restrict dan produk tidak pernah dihapus, jadi tidak mungkin menggantung.
    /// </summary>
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // ---- Snapshot: inilah yang dicetak struk dan dipakai laporan.
    public string ProductNameSnapshot { get; set; } = string.Empty;

    /// <summary>Supaya laporan per kategori tetap benar walau produknya dipindah kategori.</summary>
    public string CategoryNameSnapshot { get; set; } = string.Empty;

    /// <summary>Harga dasar produk saat penjualan terjadi.</summary>
    public decimal UnitBasePrice { get; set; }

    /// <summary>Total PriceDelta dari semua varian yang dipilih.</summary>
    public decimal VariantDeltaTotal { get; set; }

    /// <summary>UnitBasePrice + VariantDeltaTotal. Disimpan, bukan dihitung ulang saat baca.</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary>UnitPrice * Quantity. Disimpan supaya laporan tidak perlu mengalikan ulang.</summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Bentuk denormalisasi dari daftar varian: "L, Ice, Extra Shot".
    /// Dibangun sekali saat checkout. Struk dan riwayat membaca ini, sehingga
    /// jalur baca terpanas tidak perlu Include 3 level.
    /// Pasangannya (OrderItemVariant) tetap ada untuk keperluan analitik.
    /// </summary>
    public string VariantDescription { get; set; } = string.Empty;

    /// <summary>Catatan per item dari pelanggan: "less sugar".</summary>
    public string? Note { get; set; }

    public ICollection<OrderItemVariant> Variants { get; set; } = [];
}
