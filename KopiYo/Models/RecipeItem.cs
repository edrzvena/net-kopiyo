namespace KopiYo.Models;

/// <summary>
/// Satu baris resep: produk X memakai bahan Y sebanyak Z per porsi.
/// Boleh dihapus beneran (hard delete) karena tidak pernah dirujuk riwayat order —
/// yang dirujuk riwayat adalah StockMovement, bukan resepnya.
/// </summary>
public class RecipeItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    /// <summary>Presisi (18,3). Contoh: 18 g biji kopi, 150 ml susu.</summary>
    public decimal QtyPerServing { get; set; }
}
