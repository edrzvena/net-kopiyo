using System.ComponentModel.DataAnnotations;

namespace KopiYo.ViewModels.Categories;

/// <summary>Satu baris di tabel daftar kategori.</summary>
public class CategoryListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Dipakai untuk memberi tahu Admin kalau kategori masih punya produk aktif.</summary>
    public int ProductCount { get; set; }
}

/// <summary>Form Create dan Edit memakai ViewModel yang sama — field-nya identik.</summary>
public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama kategori wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama kategori maksimal 100 karakter.")]
    [Display(Name = "Nama Kategori")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Deskripsi maksimal 250 karakter.")]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; }

    [Range(0, 999, ErrorMessage = "Urutan tampil harus antara 0 dan 999.")]
    [Display(Name = "Urutan Tampil")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
