namespace KopiYo.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Urutan tombol kategori di layar POS.</summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
