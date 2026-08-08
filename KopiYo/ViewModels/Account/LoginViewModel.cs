using System.ComponentModel.DataAnnotations;

namespace KopiYo.ViewModels.Account;

/// <summary>
/// ViewModel: Views/Account/Login.cshtml bind ke kelas ini.
/// Class dengan setter (bukan record), karena model binding butuh
/// constructor tanpa parameter dan property yang bisa di-set.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Username wajib diisi.")]
    [Display(Name = "Username")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password wajib diisi.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ingat saya di perangkat ini")]
    public bool RememberMe { get; set; }
}
