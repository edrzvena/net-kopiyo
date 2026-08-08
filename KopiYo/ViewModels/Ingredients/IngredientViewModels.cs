using System.ComponentModel.DataAnnotations;
using KopiYo.Common;

namespace KopiYo.ViewModels.Ingredients;

public class IngredientListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public UnitOfMeasure Unit { get; set; }
    public decimal StockQty { get; set; }
    public decimal MinStockQty { get; set; }
    public decimal CostPerUnit { get; set; }
    public bool IsActive { get; set; }
    public int UsedInProducts { get; set; }

    public bool IsLowStock => IsActive && StockQty <= MinStockQty;
    public decimal StockValue => StockQty * CostPerUnit;
}

/// <summary>
/// Form ini SENGAJA tidak punya field StockQty.
/// Stok hanya boleh bergerak lewat IInventoryService supaya setiap perubahan
/// tercatat di StockMovements. Kalau stok bisa diketik di form edit biasa,
/// audit trail-nya langsung tidak bisa dipercaya.
/// </summary>
public class IngredientFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama bahan wajib diisi.")]
    [StringLength(100)]
    [Display(Name = "Nama Bahan")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Satuan")]
    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Gram;

    [Range(0, 10_000_000, ErrorMessage = "Stok minimum tidak valid.")]
    [Display(Name = "Stok Minimum (ambang peringatan)")]
    public decimal MinStockQty { get; set; }

    [Range(0, 100_000_000, ErrorMessage = "Harga per satuan tidak valid.")]
    [Display(Name = "Harga Beli per Satuan")]
    public decimal CostPerUnit { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    /// <summary>Hanya ditampilkan (read-only) di form edit, tidak pernah di-bind balik.</summary>
    public decimal CurrentStock { get; set; }
}

public class StockAdjustmentViewModel
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }

    [Range(0, 10_000_000, ErrorMessage = "Stok baru tidak valid.")]
    [Display(Name = "Stok Sebenarnya (hasil hitung fisik)")]
    public decimal NewQty { get; set; }

    [Required(ErrorMessage = "Alasan wajib diisi — ini yang tercatat di buku besar stok.")]
    [StringLength(200)]
    [Display(Name = "Alasan")]
    public string Reason { get; set; } = string.Empty;
}

public class StockMovementListItemViewModel
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = string.Empty;
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? OrderNumber { get; set; }
}
