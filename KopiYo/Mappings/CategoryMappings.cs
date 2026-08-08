using KopiYo.Models;
using KopiYo.ViewModels.Categories;

namespace KopiYo.Mappings;

/// <summary>
/// Mapper manual, bukan AutoMapper. Ctrl+Click di ToFormViewModel() langsung
/// memperlihatkan pemetaannya; property yang di-rename ketahuan saat compile,
/// bukan diam-diam jadi null saat runtime di produksi.
///
/// PERHATIAN: extension method di entity TIDAK BISA dipakai di dalam .Select()
/// sebuah query EF — tidak bisa diterjemahkan ke SQL. Pakai setelah data
/// ter-materialisasi, atau tulis projeksinya inline di query.
/// </summary>
public static class CategoryMappings
{
    public static CategoryFormViewModel ToFormViewModel(this Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        DisplayOrder = c.DisplayOrder,
        IsActive = c.IsActive
    };

    /// <summary>
    /// Menyalin HANYA field yang boleh diubah lewat form ke entity.
    /// Inilah pertahanan terhadap over-posting: Id dan CreatedAt tidak pernah
    /// disentuh di sini, jadi tidak ada gunanya penyerang mengirimnya.
    /// </summary>
    public static void ApplyTo(this CategoryFormViewModel vm, Category c)
    {
        c.Name = vm.Name.Trim();
        c.Description = vm.Description?.Trim();
        c.DisplayOrder = vm.DisplayOrder;
        c.IsActive = vm.IsActive;
    }
}
