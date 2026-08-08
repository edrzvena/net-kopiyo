using System.Security.Claims;
using KopiYo.Common;

namespace KopiYo.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Memeriksa username + password dan menyusun ClaimsPrincipal untuk cookie.
    /// Mengembalikan pesan yang SAMA PERSIS untuk "username tidak ada" dan
    /// "password salah" supaya tidak bisa dipakai menebak username mana yang valid.
    /// </summary>
    Task<ServiceResult<ClaimsPrincipal>> ValidateCredentialsAsync(
        string username, string password, CancellationToken ct);
}
