namespace KopiYo.Common;

/// <summary>
/// Semua enum di aplikasi ini disimpan ke database sebagai STRING (lihat konfigurasi
/// EF: .HasConversion&lt;string&gt;()). Alasannya sederhana: database POS itu sering
/// diintip langsung lewat SSMS/Rider, dan "Paid" jauh lebih berguna daripada "1".
/// Biayanya beberapa byte per baris — tidak ada artinya di skala ini.
/// </summary>
public enum UserRole
{
    /// <summary>Bisa semuanya: CRUD master data, laporan, stok, void/refund.</summary>
    Admin = 1,

    /// <summary>Hanya bisa membuka layar POS dan membuat penjualan. Tidak bisa mengubah data apa pun.</summary>
    Kasir = 2
}

public enum PaymentMethod
{
    Cash = 1,
    Qris = 2,
    Debit = 3
}

public enum OrderStatus
{
    Paid = 1,
    Voided = 2,
    Refunded = 3
}

public enum StockMovementType
{
    /// <summary>Stok masuk: pembelian bahan, atau pengembalian akibat void/refund.</summary>
    In = 1,

    /// <summary>Stok keluar: terpakai oleh penjualan.</summary>
    Out = 2,

    /// <summary>Koreksi manual oleh Admin (stock opname).</summary>
    Adjustment = 3
}

public enum UnitOfMeasure
{
    Gram = 1,
    Ml = 2,
    Pcs = 3
}

public enum VariantSelectionMode
{
    /// <summary>Hanya boleh pilih satu opsi (Ukuran, Suhu) — di POS jadi radio button.</summary>
    Single = 1,

    /// <summary>Boleh pilih beberapa opsi (Extra) — di POS jadi checkbox.</summary>
    Multiple = 2
}
