namespace KopiYo.Models;

/// <summary>
/// Bentuk ternormalisasi dari varian yang dipilih, ikut di-snapshot.
///
/// Kenapa ada ini padahal OrderItem.VariantDescription sudah menyimpan teksnya:
/// pertanyaan "berapa Extra Shot terjual bulan Juli?" mustahil dijawab dari string
/// tanpa LIKE '%Extra Shot%'. Polanya: NORMALIZED untuk analitik, DENORMALIZED
/// untuk tampilan.
/// </summary>
public class OrderItemVariant
{
    public int Id { get; set; }

    public int OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    public int VariantOptionId { get; set; }
    public VariantOption VariantOption { get; set; } = null!;

    public string GroupNameSnapshot { get; set; } = string.Empty;
    public string OptionNameSnapshot { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
}
