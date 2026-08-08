namespace KopiYo.Models;

public class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Opsional. Unique index-nya filtered (WHERE Sku IS NOT NULL) supaya boleh banyak produk tanpa SKU.</summary>
    public string? Sku { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Harga dasar SEKARANG. Ini bukan harga yang dipakai struk lama — struk memakai
    /// OrderItem.UnitBasePrice yang di-snapshot saat penjualan terjadi.
    /// </summary>
    public decimal BasePrice { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Produk tidak pernah dihapus (masih dirujuk OrderItem), hanya dinonaktifkan.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductVariantGroup> ProductVariantGroups { get; set; } = [];
    public ICollection<RecipeItem> RecipeItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
