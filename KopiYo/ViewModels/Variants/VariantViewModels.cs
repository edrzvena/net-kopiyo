using System.ComponentModel.DataAnnotations;
using KopiYo.Common;

namespace KopiYo.ViewModels.Variants;

public class VariantGroupListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public VariantSelectionMode SelectionMode { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public List<VariantOptionListItemViewModel> Options { get; set; } = [];
}

public class VariantOptionListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class VariantGroupFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama grup varian wajib diisi.")]
    [StringLength(50)]
    [Display(Name = "Nama Grup")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mode Pilihan")]
    public VariantSelectionMode SelectionMode { get; set; } = VariantSelectionMode.Single;

    [Display(Name = "Wajib dipilih kasir")]
    public bool IsRequired { get; set; }

    [Range(0, 999)]
    [Display(Name = "Urutan Tampil")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}

public class VariantOptionFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Grup")]
    public int VariantGroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama opsi wajib diisi.")]
    [StringLength(50)]
    [Display(Name = "Nama Opsi")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 10_000_000, ErrorMessage = "Tambahan harga tidak valid.")]
    [Display(Name = "Tambahan Harga")]
    public decimal PriceDelta { get; set; }

    [Range(0, 999)]
    [Display(Name = "Urutan Tampil")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
