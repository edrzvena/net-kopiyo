using KopiYo.Common;
using KopiYo.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers.Api;

/// <summary>
/// Endpoint khusus Admin. Role-nya di level CLASS: sesi Kasir yang mengarang
/// request ke sini dijawab 403 oleh middleware otorisasi sebelum body action
/// dijalankan sama sekali.
/// </summary>
[ApiController]
[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin")]
[Produces("application/json")]
public class AdminApiController(IInventoryService inventory) : ControllerBase
{
    /// <summary>Dipakai badge merah di navbar admin.</summary>
    [HttpGet("ingredients/low-stock")]
    public async Task<IActionResult> LowStock(CancellationToken ct)
    {
        var items = await inventory.GetLowStockAsync(ct);
        return Ok(items.Select(i => new
        {
            i.Id,
            i.Name,
            Unit = i.Unit.ToString(),
            i.StockQty,
            i.MinStockQty
        }));
    }
}
