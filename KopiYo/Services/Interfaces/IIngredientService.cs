using KopiYo.Common;
using KopiYo.ViewModels.Ingredients;
using KopiYo.ViewModels.Products;
using KopiYo.ViewModels.Shared;

namespace KopiYo.Services.Interfaces;

public interface IIngredientService
{
    Task<PagedList<IngredientListItemViewModel>> GetPagedAsync(
        string? search, bool lowStockOnly, int page, int pageSize, CancellationToken ct);

    Task<IngredientFormViewModel?> GetForEditAsync(int id, CancellationToken ct);
    Task<ServiceResult<int>> CreateAsync(IngredientFormViewModel vm, CancellationToken ct);

    /// <summary>
    /// SENGAJA tidak bisa mengubah StockQty — lihat komentar di IngredientFormViewModel.
    /// Stok hanya bergerak lewat IInventoryService.
    /// </summary>
    Task<ServiceResult> UpdateAsync(IngredientFormViewModel vm, CancellationToken ct);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct);
    Task<StockAdjustmentViewModel?> GetForAdjustAsync(int id, CancellationToken ct);
}

public interface IRecipeService
{
    Task<ProductRecipeViewModel?> GetRecipeAsync(int productId, CancellationToken ct);

    /// <summary>Ganti total: baris yang hilang dihapus, sisanya di-upsert, dalam satu SaveChanges.</summary>
    Task<ServiceResult> SaveRecipeAsync(
        int productId, IReadOnlyList<RecipeLineViewModel> lines, CancellationToken ct);
}
