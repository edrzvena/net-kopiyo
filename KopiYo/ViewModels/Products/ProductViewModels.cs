using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KopiYo.ViewModels.Products;

public class ProductListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; }
    public int VariantGroupCount { get; set; }
    public int RecipeItemCount { get; set; }
}

/// <summary>
/// Pilihan checkbox generik (dipakai untuk memasang grup varian ke produk).
/// </summary>
public class CheckboxItem
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public bool Selected { get; set; }
}

/// <summary>
/// ViewModel — Views/Products/Create.cshtml dan Edit.cshtml bind ke kelas ini.
///
/// Perhatikan dua property terakhir: SelectList dan List&lt;CheckboxItem&gt;.
/// Justru itulah yang membuat kelas ini BUKAN DTO — keduanya urusan tampilan
/// dan tidak punya arti kalau di-serialize jadi JSON.
/// Bandingkan dengan CatalogProductDto yang isinya cuma data mentah.
/// </summary>
public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama produk wajib diisi.")]
    [StringLength(150, ErrorMessage = "Nama produk maksimal 150 karakter.")]
    [Display(Name = "Nama Produk")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Kategori wajib dipilih.")]
    [Display(Name = "Kategori")]
    public int CategoryId { get; set; }

    [StringLength(30)]
    [Display(Name = "SKU (opsional)")]
    public string? Sku { get; set; }

    [StringLength(500)]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; }

    [Range(0, 100_000_000, ErrorMessage = "Harga tidak valid.")]
    [Display(Name = "Harga Dasar")]
    public decimal BasePrice { get; set; }

    [StringLength(300)]
    [Display(Name = "URL Gambar (opsional)")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    // ---- Khusus tampilan, tidak pernah keluar sebagai JSON:
    public SelectList? Categories { get; set; }
    public List<CheckboxItem> VariantGroups { get; set; } = [];

    /// <summary>Id grup varian yang dicentang, hasil model binding dari checkbox.</summary>
    public List<int> SelectedVariantGroupIds { get; set; } = [];
}

// ---- Editor resep -------------------------------------------------------

public class ProductRecipeViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public List<RecipeLineViewModel> Lines { get; set; } = [];

    /// <summary>Semua bahan aktif, untuk dropdown "tambah baris".</summary>
    public SelectList? Ingredients { get; set; }
}

public class RecipeLineViewModel
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = string.Empty;

    [Range(0.001, 1_000_000, ErrorMessage = "Jumlah pemakaian harus lebih dari 0.")]
    public decimal QtyPerServing { get; set; }

    public decimal StockQty { get; set; }
}
