using KopiYo.Common;
using KopiYo.Models;
using KopiYo.ViewModels.Ingredients;
using KopiYo.ViewModels.Shared;

namespace KopiYo.Services.Interfaces;

public interface IInventoryService
{
    /// <summary>
    /// Membaca resep semua produk yang dipesan dan menjumlahkan total kebutuhan
    /// per bahan. Dua Latte + satu Cappuccino digabung jadi satu angka biji kopi.
    /// </summary>
    Task<IReadOnlyDictionary<int, decimal>> BuildConsumptionAsync(
        IReadOnlyList<(int ProductId, int Quantity)> lines, CancellationToken ct);

    /// <summary>
    /// Dipanggil DI DALAM transaksi checkout. Mengurangi Ingredient.StockQty dan
    /// menambahkan baris StockMovement ke graph order.
    /// TIDAK memanggil SaveChanges — pemanggil yang memiliki unit of work-nya.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<string>>> ConsumeForOrderAsync(
        Order order, IReadOnlyDictionary<int, decimal> consumption, int userId, CancellationToken ct);

    /// <summary>
    /// Membalik pemakaian bahan sebuah order menjadi movement masuk (untuk void/refund).
    /// Juga tidak memanggil SaveChanges.
    /// </summary>
    Task<ServiceResult> RestoreForOrderAsync(Order order, int userId, string reason, CancellationToken ct);

    /// <summary>Koreksi manual oleh Admin. Yang ini punya transaksi dan SaveChanges sendiri.</summary>
    Task<ServiceResult> AdjustAsync(
        int ingredientId, decimal newQty, string reason, int userId, CancellationToken ct);

    Task<PagedList<StockMovementListItemViewModel>> GetMovementsAsync(
        int? ingredientId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<IngredientListItemViewModel>> GetLowStockAsync(CancellationToken ct);
}
