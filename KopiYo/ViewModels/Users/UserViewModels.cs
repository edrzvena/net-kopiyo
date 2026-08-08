using System.ComponentModel.DataAnnotations;
using KopiYo.Common;

namespace KopiYo.ViewModels.Users;

public class UserListItemViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderCount { get; set; }
}

public class UserFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Username wajib diisi.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username 3-50 karakter.")]
    [RegularExpression("^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username hanya boleh huruf, angka, titik, garis bawah, dan strip.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    [StringLength(100)]
    [Display(Name = "Nama Lengkap")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Role")]
    public UserRole Role { get; set; } = UserRole.Kasir;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Hanya dipakai saat Create. Untuk Edit, password diubah lewat layar
    /// Reset Password terpisah — supaya form edit biasa tidak pernah
    /// tidak sengaja menimpa password dengan string kosong.
    /// </summary>
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter.")]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Konfirmasi password tidak sama.")]
    [Display(Name = "Ulangi Password")]
    public string? ConfirmPassword { get; set; }

    public bool IsNew => Id == 0;
}

public class ResetPasswordViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password baru wajib diisi.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter.")]
    [Display(Name = "Password Baru")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi password tidak sama.")]
    [Display(Name = "Ulangi Password Baru")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
