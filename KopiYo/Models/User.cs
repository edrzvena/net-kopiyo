using KopiYo.Common;

namespace KopiYo.Models;

public class User
{
    public int Id { get; set; }

    /// <summary>Unik. Dipakai untuk login.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Nama yang tampil di navbar dan di-snapshot ke Order.CashierNameSnapshot.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Hasil PasswordHasher&lt;User&gt; (format v3, PBKDF2 + salt acak). Bukan plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Kasir;

    /// <summary>User tidak pernah dihapus — hanya dinonaktifkan, karena Order-nya harus tetap utuh.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Order> OrdersAsCashier { get; set; } = [];
    public ICollection<Order> OrdersReversed { get; set; } = [];
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
