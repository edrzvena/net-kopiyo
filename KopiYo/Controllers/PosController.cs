using KopiYo.Common;
using KopiYo.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

/// <summary>
/// Layar kasir. [Authorize] tanpa role: Admin DAN Kasir sama-sama boleh berjualan.
/// </summary>
[Authorize]
public class PosController(IOrderService orders) : Controller
{
    public IActionResult Index() => View();

    /// <summary>
    /// Cetak ulang struk. Kasir hanya boleh membuka order miliknya sendiri;
    /// Admin boleh semua. Pemeriksaan ini tidak bisa digantikan atribut apa pun
    /// karena bergantung pada isi baris datanya (IDOR).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Receipt(int id, CancellationToken ct)
    {
        var ownerId = await orders.GetCashierIdAsync(id, ct);
        if (ownerId is null) return NotFound();
        if (!User.IsAdmin() && ownerId != User.GetUserId()) return Forbid();

        var receipt = await orders.GetReceiptAsync(id, ct);
        return receipt is null ? NotFound() : View(receipt);
    }
}
