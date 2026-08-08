using KopiYo.Common;
using KopiYo.DTOs.Pos;
using KopiYo.ViewModels.Orders;
using KopiYo.ViewModels.Shared;

namespace KopiYo.Services.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Membuat order dalam satu transaksi. Semua harga dihitung ulang di server
    /// dari data database — apa pun yang dikirim client soal uang diabaikan.
    /// Mengembalikan ErrorKind.Conflict (-> HTTP 409) kalau stok bahan tidak cukup.
    /// </summary>
    Task<ServiceResult<OrderResultDto>> CreateOrderAsync(
        CreateOrderDto dto, int cashierId, CancellationToken ct);

    Task<ReceiptDto?> GetReceiptAsync(int orderId, CancellationToken ct);

    /// <summary>
    /// Id kasir pemilik order, untuk pemeriksaan IDOR sebelum menampilkan struk.
    /// Null kalau order-nya tidak ada.
    /// </summary>
    Task<int?> GetCashierIdAsync(int orderId, CancellationToken ct);

    Task<PagedList<OrderListItemViewModel>> GetPagedAsync(
        DateOnly? from, DateOnly? to, int? cashierId, OrderStatus? status, string? search,
        int page, int pageSize, CancellationToken ct);

    Task<OrderDetailsViewModel?> GetDetailsAsync(int orderId, CancellationToken ct);

    Task<ReverseOrderViewModel?> GetForReverseAsync(int orderId, bool isVoid, CancellationToken ct);

    /// <summary>
    /// Void = pembatalan salah input di hari yang sama, stok dikembalikan.
    /// Refund = uang dikembalikan setelahnya, stok opsional (minuman yang sudah
    /// dibuat biasanya dibuang, jadi bahannya benar-benar hilang).
    /// Keduanya HANYA untuk Admin, dan idempotent-guarded.
    /// </summary>
    Task<ServiceResult> ReverseOrderAsync(
        int orderId, bool isVoid, string reason, bool restoreStock, int adminUserId, CancellationToken ct);
}
