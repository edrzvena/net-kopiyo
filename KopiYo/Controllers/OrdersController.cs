using KopiYo.Common;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

/// <summary>
/// Riwayat transaksi dan pembalikannya. SELURUHNYA Admin-only.
///
/// Kasir sengaja tidak bisa void: "ring penjualan, terima uang tunai, void
/// order, selisihnya masuk kantong" adalah jalur kebocoran kas paling umum di
/// F&amp;B. Semua POS sungguhan menaruh void di belakang supervisor. Matriks
/// role-nya jadi gampang dijelaskan: Kasir membuat, Admin mengoreksi.
/// </summary>
[Authorize(Roles = AppConstants.Roles.Admin)]
public class OrdersController(IOrderService orders) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        DateOnly? from, DateOnly? to, int? cashierId, OrderStatus? status, string? search,
        int page = 1, CancellationToken ct = default)
    {
        ViewData["From"] = from;
        ViewData["To"] = to;
        ViewData["Status"] = status;
        ViewData["Search"] = search;

        return View(await orders.GetPagedAsync(
            from, to, cashierId, status, search, page, AppConstants.DefaultPageSize, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var vm = await orders.GetDetailsAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id, CancellationToken ct)
    {
        var receipt = await orders.GetReceiptAsync(id, ct);
        // Admin boleh melihat struk siapa pun, jadi tidak ada pemeriksaan pemilik
        // di sini — berbeda dengan PosController.Receipt yang dipakai kasir.
        return receipt is null ? NotFound() : View("~/Views/Pos/Receipt.cshtml", receipt);
    }

    [HttpGet]
    public async Task<IActionResult> Reverse(int id, bool isVoid, CancellationToken ct)
    {
        var vm = await orders.GetForReverseAsync(id, isVoid, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Reverse(ReverseOrderViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await orders.ReverseOrderAsync(
            vm.OrderId, vm.IsVoid, vm.Reason, vm.RestoreStock, User.GetUserId(), ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] =
            $"Order {vm.OrderNumber} berhasil di-{(vm.IsVoid ? "batalkan" : "refund")}.";
        return RedirectToAction(nameof(Details), new { id = vm.OrderId });
    }
}
