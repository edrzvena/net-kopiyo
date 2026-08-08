using KopiYo.Common;

namespace KopiYo.Models;

public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public UnitOfMeasure Unit { get; set; }

    /// <summary>
    /// Presisi (18,3), bukan (18,2): resep bisa 7,5 g biji kopi.
    /// Nilai ini HANYA boleh diubah lewat InventoryService, tidak pernah lewat
    /// form edit bahan — supaya setiap perubahan pasti tercatat di StockMovements.
    /// </summary>
    public decimal StockQty { get; set; }

    /// <summary>Ambang peringatan stok menipis.</summary>
    public decimal MinStockQty { get; set; }

    /// <summary>Harga beli per unit, untuk perhitungan nilai persediaan.</summary>
    public decimal CostPerUnit { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<RecipeItem> RecipeItems { get; set; } = [];
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
