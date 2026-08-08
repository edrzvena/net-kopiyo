namespace KopiYo.Common;

/// <summary>
/// Di-bind dari section "KopiYo" di appsettings.json lewat Configure&lt;KopiYoSettings&gt;.
/// Nilai pajak/service di sini adalah nilai YANG BERLAKU SEKARANG; saat checkout
/// nilainya di-snapshot ke baris Order, sehingga struk lama tetap tercetak dengan
/// persentase yang berlaku waktu itu meskipun nilai di sini diubah.
/// </summary>
public sealed class KopiYoSettings
{
    public const string SectionName = "KopiYo";

    public string CafeName { get; set; } = "KopiYo";
    public string CafeAddress { get; set; } = "";
    public string CafePhone { get; set; } = "";

    /// <summary>PB1 / pajak restoran, persen. 10 = 10%.</summary>
    public decimal TaxPercent { get; set; } = 10m;

    /// <summary>Service charge, persen. 0 = tidak dipakai.</summary>
    public decimal ServiceChargePercent { get; set; }

    /// <summary>
    /// true  = penjualan DITOLAK (409) kalau stok bahan tidak cukup — seluruh transaksi rollback.
    /// false = penjualan tetap jalan sampai stok minus, tapi mengembalikan warning ke layar POS.
    /// Default true: stok minus diam-diam merusak semua angka turunan dan baru ketahuan saat tutup buku.
    /// </summary>
    public bool BlockSaleOnInsufficientStock { get; set; } = true;

    public bool LowStockWarningEnabled { get; set; } = true;
}
