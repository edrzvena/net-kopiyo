using KopiYo.Common;
using KopiYo.DTOs.Pos;
using KopiYo.ViewModels.Products;
using KopiYo.ViewModels.Shared;

namespace KopiYo.Services.Interfaces;

public interface IProductService
{
    Task<PagedList<ProductListItemViewModel>> GetPagedAsync(
        string? search, int? categoryId, bool? isActive, int page, int pageSize, CancellationToken ct);

    /// <summary>Menyiapkan form kosong lengkap dengan dropdown kategori dan daftar grup varian.</summary>
    Task<ProductFormViewModel> BuildCreateFormAsync(CancellationToken ct);

    Task<ProductFormViewModel?> GetForEditAsync(int id, CancellationToken ct);

    /// <summary>Mengisi ulang SelectList dan daftar checkbox setelah validasi gagal.</summary>
    Task RepopulateFormAsync(ProductFormViewModel vm, CancellationToken ct);

    Task<ServiceResult<int>> CreateAsync(ProductFormViewModel vm, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(ProductFormViewModel vm, CancellationToken ct);
    Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct);

    /// <summary>
    /// Katalog untuk layar kasir: hanya data AKTIF, sekali ambil saat halaman dibuka.
    /// </summary>
    Task<CatalogDto> GetPosCatalogAsync(CancellationToken ct);
}
