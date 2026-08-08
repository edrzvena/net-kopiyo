namespace KopiYo.Models;

/// <summary>
/// Tabel join murni: grup varian mana yang berlaku untuk produk mana.
/// Ini satu dari dua entity yang boleh dihapus beneran (hard delete), karena
/// tidak pernah dirujuk oleh riwayat order.
/// </summary>
public class ProductVariantGroup
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int VariantGroupId { get; set; }
    public VariantGroup VariantGroup { get; set; } = null!;
}
