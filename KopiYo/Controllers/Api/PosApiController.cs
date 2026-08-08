using KopiYo.Common;
using KopiYo.DTOs.Pos;
using KopiYo.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers.Api;

/// <summary>
/// API untuk layar kasir. [Authorize] tanpa role: Admin dan Kasir sama-sama boleh.
/// Endpoint khusus Admin ada di AdminApiController yang terpisah, dengan
/// [Authorize(Roles = "Admin")] di level class — sehingga request buatan tangan
/// dari sesi Kasir dijawab 403 oleh middleware sebelum action-nya jalan.
/// </summary>
[ApiController]
[Authorize]
[Route("api/pos")]
[Produces("application/json")]
public class PosApiController(IProductService products, IOrderService orders) : ControllerBase
{
    [HttpGet("catalog")]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
        => Ok(await products.GetPosCatalogAsync(ct));

    [HttpPost("checkout")]
    [ProducesResponseType<OrderResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        var result = await orders.CreateOrderAsync(dto, User.GetUserId(), ct);
        if (result.Succeeded) return Ok(result.Value);

        var error = new ApiErrorDto(result.Errors);
        return result.Kind switch
        {
            ErrorKind.Conflict => Conflict(error),   // stok bahan kurang
            ErrorKind.NotFound => NotFound(error),
            _ => BadRequest(error)
        };
    }

    [HttpGet("orders/{id:int}/receipt")]
    [ProducesResponseType<ReceiptDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceipt(int id, CancellationToken ct)
    {
        // Atribut [Authorize] tidak bisa menangani otorisasi tingkat OBJEK.
        // Tanpa pemeriksaan ini, kasir Budi bisa membaca struk kasir Ani cukup
        // dengan menebak id-nya (IDOR).
        var ownerId = await orders.GetCashierIdAsync(id, ct);
        if (ownerId is null) return NotFound();
        if (!User.IsAdmin() && ownerId != User.GetUserId()) return Forbid();

        var receipt = await orders.GetReceiptAsync(id, ct);
        return receipt is null ? NotFound() : Ok(receipt);
    }

    /// <summary>
    /// Validasi model otomatis dari [ApiController] menghasilkan ValidationProblemDetails,
    /// bentuk yang berbeda dari ApiErrorDto. Ini menyeragamkannya jadi satu bentuk
    /// supaya pos.js cukup menangani satu format error.
    /// </summary>
    public static IActionResult MapModelStateErrors(ActionContext context)
    {
        var errors = context.ModelState
            .SelectMany(kv => kv.Value?.Errors.Select(e => e.ErrorMessage) ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .DefaultIfEmpty("Data yang dikirim tidak valid.")
            .ToList();

        return new BadRequestObjectResult(new ApiErrorDto(errors));
    }
}
