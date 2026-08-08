using KopiYo.Common;
using KopiYo.ViewModels.Categories;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KopiYo.Services.Interfaces;

public interface ICategoryService
{
    /// <param name="activeOnly">
    /// true untuk layar POS (hanya kategori aktif), false untuk layar admin
    /// (semua, termasuk yang nonaktif, supaya bisa diaktifkan lagi).
    ///
    /// Ini sengaja parameter eksplisit, bukan HasQueryFilter global: filter global
    /// itu tak terlihat dan akan ikut menyembunyikan data dari layar admin,
    /// lalu kamu berperang dengan IgnoreQueryFilters() di mana-mana.
    /// </param>
    Task<IReadOnlyList<CategoryListItemViewModel>> GetAllAsync(bool activeOnly, CancellationToken ct);

    Task<CategoryFormViewModel?> GetForEditAsync(int id, CancellationToken ct);
    Task<ServiceResult<int>> CreateAsync(CategoryFormViewModel vm, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(CategoryFormViewModel vm, CancellationToken ct);

    /// <summary>Nonaktifkan / aktifkan. Tidak ada Delete — kategori dirujuk produk dan riwayat.</summary>
    Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct);

    Task<SelectList> GetSelectListAsync(int? selectedId, CancellationToken ct);
}
