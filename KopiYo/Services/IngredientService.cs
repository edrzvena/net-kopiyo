using KopiYo.Common;
using KopiYo.Data;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Ingredients;
using KopiYo.ViewModels.Shared;

namespace KopiYo.Services;

public sealed class IngredientService(AppDbContext db, IDateTimeProvider clock) : IIngredientService
{
    public async Task<PagedList<IngredientListItemViewModel>> GetPagedAsync(
        string? search, bool lowStockOnly, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Ingredients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Name.Contains(term));
        }

        if (lowStockOnly)
            query = query.Where(i => i.IsActive && i.StockQty <= i.MinStockQty);

        var projected = query
            .OrderBy(i => i.Name)
            .Select(i => new IngredientListItemViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Unit = i.Unit,
                StockQty = i.StockQty,
                MinStockQty = i.MinStockQty,
                CostPerUnit = i.CostPerUnit,
                IsActive = i.IsActive,
                UsedInProducts = i.RecipeItems.Count
            });

        return await PagedList<IngredientListItemViewModel>.CreateAsync(projected, page, pageSize, ct);
    }

    public async Task<IngredientFormViewModel?> GetForEditAsync(int id, CancellationToken ct)
        => await db.Ingredients.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new IngredientFormViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Unit = i.Unit,
                MinStockQty = i.MinStockQty,
                CostPerUnit = i.CostPerUnit,
                IsActive = i.IsActive,
                CurrentStock = i.StockQty
            })
            .FirstOrDefaultAsync(ct);

    public async Task<ServiceResult<int>> CreateAsync(IngredientFormViewModel vm, CancellationToken ct)
    {
        var name = vm.Name.Trim();
        if (await db.Ingredients.AnyAsync(i => i.Name == name, ct))
            return ServiceResult<int>.Fail($"Bahan '{name}' sudah ada.");

        var ingredient = new Ingredient
        {
            Name = name,
            Unit = vm.Unit,
            // Bahan baru selalu mulai dari stok 0. Pengisian awal dilakukan lewat
            // Sesuaikan Stok, supaya jumlah masuk pertamanya pun tercatat di ledger.
            StockQty = 0m,
            MinStockQty = vm.MinStockQty,
            CostPerUnit = vm.CostPerUnit,
            IsActive = vm.IsActive,
            CreatedAt = clock.NowWib
        };

        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync(ct);
        return ServiceResult<int>.Ok(ingredient.Id);
    }

    public async Task<ServiceResult> UpdateAsync(IngredientFormViewModel vm, CancellationToken ct)
    {
        var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == vm.Id, ct);
        if (ingredient is null) return ServiceResult.Fail("Bahan tidak ditemukan.", ErrorKind.NotFound);

        var name = vm.Name.Trim();
        if (await db.Ingredients.AnyAsync(i => i.Name == name && i.Id != vm.Id, ct))
            return ServiceResult.Fail($"Bahan '{name}' sudah dipakai.");

        // Perhatikan: StockQty TIDAK disentuh di sini. Itu bukan kelalaian.
        ingredient.Name = name;
        ingredient.Unit = vm.Unit;
        ingredient.MinStockQty = vm.MinStockQty;
        ingredient.CostPerUnit = vm.CostPerUnit;
        ingredient.IsActive = vm.IsActive;
        ingredient.UpdatedAt = clock.NowWib;

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (ingredient is null) return ServiceResult.Fail("Bahan tidak ditemukan.", ErrorKind.NotFound);

        if (!isActive)
        {
            var usedIn = await db.RecipeItems.CountAsync(r => r.IngredientId == id, ct);
            if (usedIn > 0)
                return ServiceResult.Fail(
                    $"Bahan ini masih dipakai di {usedIn} resep. Hapus dari resepnya dulu.");
        }

        ingredient.IsActive = isActive;
        ingredient.UpdatedAt = clock.NowWib;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<StockAdjustmentViewModel?> GetForAdjustAsync(int id, CancellationToken ct)
        => await db.Ingredients.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new StockAdjustmentViewModel
            {
                IngredientId = i.Id,
                IngredientName = i.Name,
                UnitLabel = i.Unit.ToString(),
                CurrentStock = i.StockQty,
                NewQty = i.StockQty
            })
            .FirstOrDefaultAsync(ct);
}
