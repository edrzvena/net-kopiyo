namespace KopiYo.Models;

/// <summary>Opsi di dalam grup: S / M / L, Hot / Ice, Extra Shot.</summary>
public class VariantOption
{
    public int Id { get; set; }

    public int VariantGroupId { get; set; }
    public VariantGroup VariantGroup { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Tambahan harga terhadap Product.BasePrice. Boleh 0 (mis. ukuran S).</summary>
    public decimal PriceDelta { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<OrderItemVariant> OrderItemVariants { get; set; } = [];
}
