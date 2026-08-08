using System.Data;
using KopiYo.Common;
using KopiYo.Data;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Ingredients;
using KopiYo.ViewModels.Shared;
using Microsoft.Extensions.Options;

namespace KopiYo.Services;

public sealed class InventoryService(
    AppDbContext db,
    IDateTimeProvider clock,
    IOptions<KopiYoSettings> settings) : IInventoryService
{
    private readonly KopiYoSettings _settings = settings.Value;

    public async Task<IReadOnlyDictionary<int, decimal>> BuildConsumptionAsync(
        IReadOnlyList<(int ProductId, int Quantity)> lines, CancellationToken ct)
    {
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();

        var recipes = await db.RecipeItems.AsNoTracking()
            .Where(r => productIds.Contains(r.ProductId))
            .Select(r => new { r.ProductId, r.IngredientId, r.QtyPerServing })
            .ToListAsync(ct);

        var need = new Dictionary<int, decimal>();
        foreach (var line in lines)
        {
            foreach (var recipe in recipes.Where(r => r.ProductId == line.ProductId))
            {
                var amount = recipe.QtyPerServing * line.Quantity;
                need[recipe.IngredientId] = need.GetValueOrDefault(recipe.IngredientId) + amount;
            }
        }

        return need;
    }

    public async Task<ServiceResult<IReadOnlyList<string>>> ConsumeForOrderAsync(
        Order order, IReadOnlyDictionary<int, decimal> consumption, int userId, CancellationToken ct)
    {
        var warnings = new List<string>();

        // Produk tanpa resep (mis. kue titipan) sah-sah saja: tidak ada yang dipotong.
        if (consumption.Count == 0)
            return ServiceResult<IReadOnlyList<string>>.Ok(warnings);

        var ids = consumption.Keys.ToList();

        // UPDLOCK saat MEMBACA. Tanpa ini dua checkout bersamaan sama-sama membaca
        // stok 5, sama-sama mengurangi 5, dan stok berakhir di -5 padahal seharusnya
        // penjualan kedua ditolak.
        var ingredients = await db.Ingredients
            .FromSql($"SELECT * FROM Ingredients WITH (UPDLOCK)")
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var shortages = new List<string>();
        foreach (var (ingredientId, needed) in consumption)
        {
            if (!ingredients.TryGetValue(ingredientId, out var ing))
                return ServiceResult<IReadOnlyList<string>>.Fail(
                    $"Bahan #{ingredientId} pada resep tidak ditemukan.", ErrorKind.NotFound);

            if (ing.StockQty < needed)
            {
                var message =
                    $"{ing.Name} (butuh {needed.ToQty()} {ing.Unit}, tersedia {ing.StockQty.ToQty()} {ing.Unit})";

                if (_settings.BlockSaleOnInsufficientStock) shortages.Add(message);
                else warnings.Add($"Stok minus: {message}");
            }
        }

        // Conflict -> HTTP 409. Seluruh transaksi checkout di-rollback: tidak ada order,
        // tidak ada perubahan stok, dan nomor order tidak terpakai sia-sia.
        if (shortages.Count > 0)
            return ServiceResult<IReadOnlyList<string>>.Fail(
                "Stok bahan tidak mencukupi: " + string.Join("; ", shortages), ErrorKind.Conflict);

        var now = clock.NowWib;
        foreach (var (ingredientId, needed) in consumption)
        {
            var ing = ingredients[ingredientId];
            var before = ing.StockQty;
            ing.StockQty = before - needed;   // tracked -> jadi UPDATE saat SaveChanges

            // Ditambahkan ke graph order, bukan langsung ke DbSet: EF yang mengisi
            // OrderId setelah order-nya dapat identity, tanpa round-trip tambahan.
            order.StockMovements.Add(new StockMovement
            {
                IngredientId = ingredientId,
                MovementType = StockMovementType.Out,
                Quantity = needed,
                StockBefore = before,
                StockAfter = ing.StockQty,
                Reason = $"Penjualan {order.OrderNumber}",
                UserId = userId,
                CreatedAt = now
            });
        }

        return ServiceResult<IReadOnlyList<string>>.Ok(warnings);
    }

    public async Task<ServiceResult> RestoreForOrderAsync(
        Order order, int userId, string reason, CancellationToken ct)
    {
        // Yang dibalik adalah movement Out milik order ini, bukan resep produknya
        // sekarang — resepnya bisa saja sudah diubah sejak penjualan terjadi.
        var outMovements = await db.StockMovements
            .Where(m => m.OrderId == order.Id && m.MovementType == StockMovementType.Out)
            .ToListAsync(ct);

        if (outMovements.Count == 0) return ServiceResult.Ok();

        var ids = outMovements.Select(m => m.IngredientId).Distinct().ToList();
        var ingredients = await db.Ingredients
            .FromSql($"SELECT * FROM Ingredients WITH (UPDLOCK)")
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var now = clock.NowWib;
        foreach (var group in outMovements.GroupBy(m => m.IngredientId))
        {
            var ing = ingredients[group.Key];
            var total = group.Sum(m => m.Quantity);
            var before = ing.StockQty;
            ing.StockQty = before + total;

            db.StockMovements.Add(new StockMovement
            {
                IngredientId = group.Key,
                OrderId = order.Id,
                MovementType = StockMovementType.In,
                Quantity = total,
                StockBefore = before,
                StockAfter = ing.StockQty,
                Reason = reason,
                UserId = userId,
                CreatedAt = now
            });
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AdjustAsync(
        int ingredientId, decimal newQty, string reason, int userId, CancellationToken ct)
    {
        if (newQty < 0) return ServiceResult.Fail("Stok tidak boleh negatif.");

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            var ing = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == ingredientId, ct);
            if (ing is null) return ServiceResult.Fail("Bahan tidak ditemukan.", ErrorKind.NotFound);

            var before = ing.StockQty;
            var difference = newQty - before;
            if (difference == 0) return ServiceResult.Fail("Stok tidak berubah, tidak ada yang dicatat.");

            ing.StockQty = newQty;
            ing.UpdatedAt = clock.NowWib;

            db.StockMovements.Add(new StockMovement
            {
                IngredientId = ingredientId,
                MovementType = StockMovementType.Adjustment,
                Quantity = Math.Abs(difference),   // selalu positif; arahnya terbaca dari Before/After
                StockBefore = before,
                StockAfter = newQty,
                Reason = reason.Trim(),
                UserId = userId,
                CreatedAt = clock.NowWib
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ServiceResult.Ok();
        });
    }

    public async Task<PagedList<StockMovementListItemViewModel>> GetMovementsAsync(
        int? ingredientId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var query = db.StockMovements.AsNoTracking();

        if (ingredientId is > 0) query = query.Where(m => m.IngredientId == ingredientId);
        if (from.HasValue) query = query.Where(m => m.CreatedAt >= from.Value);
        // Half-open: < (to + 1 hari), bukan <= to. Kalau memakai <=, semua movement
        // setelah jam 00:00:00.000 di hari terakhir akan hilang dari hasil.
        if (to.HasValue) query = query.Where(m => m.CreatedAt < to.Value.Date.AddDays(1));

        var projected = query
            // Tiebreaker Id penting: beberapa movement dalam satu transaksi punya
            // CreatedAt identik, dan tanpa itu paging bisa mengulang atau melewati baris.
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Select(m => new StockMovementListItemViewModel
            {
                Id = m.Id,
                CreatedAt = m.CreatedAt,
                IngredientName = m.Ingredient.Name,
                UnitLabel = m.Ingredient.Unit.ToString(),
                MovementType = m.MovementType,
                Quantity = m.Quantity,
                StockBefore = m.StockBefore,
                StockAfter = m.StockAfter,
                Reason = m.Reason,
                UserName = m.User.FullName,
                OrderNumber = m.Order != null ? m.Order.OrderNumber : null
            });

        return await PagedList<StockMovementListItemViewModel>.CreateAsync(projected, page, pageSize, ct);
    }

    public async Task<IReadOnlyList<IngredientListItemViewModel>> GetLowStockAsync(CancellationToken ct)
        => await db.Ingredients.AsNoTracking()
            .Where(i => i.IsActive && i.StockQty <= i.MinStockQty)
            .OrderBy(i => i.Name)
            .Select(i => new IngredientListItemViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Unit = i.Unit,
                StockQty = i.StockQty,
                MinStockQty = i.MinStockQty,
                CostPerUnit = i.CostPerUnit,
                IsActive = i.IsActive
            })
            .ToListAsync(ct);
}
