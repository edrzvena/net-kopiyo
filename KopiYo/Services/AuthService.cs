using System.Security.Claims;
using KopiYo.Common;
using KopiYo.Data;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace KopiYo.Services;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher<User> hasher,
    ILogger<AuthService> logger) : IAuthService
{
    private const string GenericFailure = "Username atau password salah.";

    public async Task<ServiceResult<ClaimsPrincipal>> ValidateCredentialsAsync(
        string username, string password, CancellationToken ct)
    {
        var normalized = username.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalized, ct);

        if (user is null)
            return ServiceResult<ClaimsPrincipal>.Fail(GenericFailure);

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
            return ServiceResult<ClaimsPrincipal>.Fail(GenericFailure);

        // Akun nonaktif memang boleh dibedakan pesannya: password-nya sudah terbukti
        // benar, jadi tidak ada informasi baru yang bocor ke penebak.
        if (!user.IsActive)
            return ServiceResult<ClaimsPrincipal>.Fail("Akun ini sudah dinonaktifkan. Hubungi Admin.");

        // Hash lama (iterasi lebih rendah) di-upgrade diam-diam saat login berhasil.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Password hash user {Username} di-upgrade ke format terbaru.", user.Username);
        }

        return ServiceResult<ClaimsPrincipal>.Ok(BuildPrincipal(user));
    }

    private static ClaimsPrincipal BuildPrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(AppConstants.FullNameClaim, user.FullName),

            // ClaimTypes.Role inilah yang dibaca [Authorize(Roles = ...)] dan
            // User.IsInRole(). Salah tipe claim = role-nya tidak pernah cocok.
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
