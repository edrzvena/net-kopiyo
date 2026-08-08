using KopiYo.Common;
using KopiYo.Data;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Products;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KopiYo.Services;

public sealed class RecipeService(AppDbContext db) : IRecipeService
{
    public async Task<ProductRecipeViewModel?> GetRecipeAsync(int productId, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.Name })
            .FirstOrDefaultAsync(ct);

        if (product is null) return null;

        var lines = await db.RecipeItems.AsNoTracking()
            .Where(r => r.ProductId == productId)
            .OrderBy(r => r.Ingredient.Name)
            .Select(r => new RecipeLineViewModel
            {
                IngredientId = r.IngredientId,
                IngredientName = r.Ingredient.Name,
                UnitLabel = r.Ingredient.Unit.ToString(),
                QtyPerServing = r.QtyPerServing,
                StockQty = r.Ingredient.StockQty
            })
            .ToListAsync(ct);

        var ingredients = await db.Ingredients.AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .Select(i => new { i.Id, Label = i.Name + " (" + i.Unit.ToString() + ")" })
            .ToListAsync(ct);

        return new ProductRecipeViewModel
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Lines = lines,
            Ingredients = new SelectList(ingredients, "Id", "Label")
        };
    }

    public async Task<ServiceResult> SaveRecipeAsync(
        int productId, IReadOnlyList<RecipeLineViewModel> lines, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
            return ServiceResult.Fail("Produk tidak ditemukan.", ErrorKind.NotFound);

        // Baris dengan jumlah <= 0 dianggap "dihapus" oleh Admin.
        var desired = lines
            .Where(l => l.IngredientId > 0 && l.QtyPerServing > 0)
            .GroupBy(l => l.IngredientId)
            .ToDictionary(g => g.Key, g => g.Last().QtyPerServing);

        var validIds = await db.Ingredients
            .Where(i => desired.Keys.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync(ct);

        if (validIds.Count != desired.Count)
            return ServiceResult.Fail("Ada bahan yang dipilih tapi tidak ditemukan.");

        var current = await db.RecipeItems.Where(r => r.ProductId == productId).ToListAsync(ct);

        // Hard delete di sini BENAR: RecipeItem adalah baris relasi murni yang tidak
        // pernah dirujuk riwayat order. Yang dirujuk riwayat adalah StockMovement,
        // dan itu append-only.
        foreach (var row in current.Where(r => !desired.ContainsKey(r.IngredientId)))
            db.RecipeItems.Remove(row);

        foreach (var row in current.Where(r => desired.ContainsKey(r.IngredientId)))
            row.QtyPerServing = desired[row.IngredientId];

        var existingIds = current.Select(r => r.IngredientId).ToHashSet();
        foreach (var (ingredientId, qty) in desired.Where(d => !existingIds.Contains(d.Key)))
            db.RecipeItems.Add(new RecipeItem
            {
                ProductId = productId,
                IngredientId = ingredientId,
                QtyPerServing = qty
            });

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
