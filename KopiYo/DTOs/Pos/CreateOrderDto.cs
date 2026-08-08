using System.ComponentModel.DataAnnotations;
using KopiYo.Common;

namespace KopiYo.DTOs.Pos;

/// <summary>
/// Payload checkout dari layar kasir.
///
/// PERHATIKAN: tidak ada satu pun field harga di sini, dan itu disengaja.
/// Client hanya mengirim ID dan jumlah; setiap rupiah dihitung ulang di server
/// dari data database. Menambahkan field "price" ke DTO ini akan langsung
/// membuka celah manipulasi harga dari DevTools.
/// </summary>
public sealed record CreateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Keranjang kosong.")]
    public List<CreateOrderItemDto> Items { get; init; } = [];

    [Range(0, 100, ErrorMessage = "Diskon persen harus 0-100.")]
    public decimal DiscountPercent { get; init; }

    /// <summary>Diskon nominal. Hanya dipakai kalau DiscountPercent = 0.</summary>
    [Range(0, 1_000_000_000)]
    public decimal DiscountAmount { get; init; }

    [Required]
    public PaymentMethod PaymentMethod { get; init; }

    [Range(0, 1_000_000_000)]
    public decimal AmountPaid { get; init; }

    [StringLength(250)]
    public string? Note { get; init; }
}

public sealed record CreateOrderItemDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 999, ErrorMessage = "Jumlah item harus 1-999.")]
    public int Quantity { get; init; }

    public List<int> VariantOptionIds { get; init; } = [];

    [StringLength(200)]
    public string? Note { get; init; }
}
