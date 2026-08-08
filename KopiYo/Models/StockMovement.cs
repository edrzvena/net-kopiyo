using KopiYo.Common;

namespace KopiYo.Models;

/// <summary>
/// Buku besar pergerakan stok. APPEND-ONLY: tidak pernah di-update, tidak pernah dihapus.
/// Semua perubahan Ingredient.StockQty wajib lewat sini — itulah yang membuat
/// audit trail-nya bisa dipercaya.
/// </summary>
public class StockMovement
{
    public int Id { get; set; }

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    public StockMovementType MovementType { get; set; }

    /// <summary>SELALU positif. Arahnya ditentukan oleh MovementType, bukan oleh tanda.</summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// StockBefore/StockAfter adalah fitur utamanya: kalau enam bulan lagi StockQty
    /// terlihat salah, kamu bisa memutar ulang ledger ini dan menemukan persis di
    /// movement mana angkanya mulai melenceng. Harganya cuma dua kolom.
    /// </summary>
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>Null untuk penyesuaian manual oleh Admin.</summary>
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
