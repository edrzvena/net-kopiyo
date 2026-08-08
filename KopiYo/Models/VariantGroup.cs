using KopiYo.Common;

namespace KopiYo.Models;

/// <summary>
/// Grup varian global: "Ukuran", "Suhu", "Extra".
/// Didefinisikan SEKALI lalu dipasang ke banyak produk lewat ProductVariantGroup,
/// bukan diduplikasi per produk. Mengubah harga "Extra Shot" jadi update satu baris,
/// bukan 40 baris.
/// </summary>
public class VariantGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Single -> radio button di POS. Multiple -> checkbox.</summary>
    public VariantSelectionMode SelectionMode { get; set; } = VariantSelectionMode.Single;

    /// <summary>Kalau true, kasir wajib memilih satu opsi sebelum item bisa masuk keranjang.</summary>
    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<VariantOption> Options { get; set; } = [];
    public ICollection<ProductVariantGroup> ProductVariantGroups { get; set; } = [];
}
