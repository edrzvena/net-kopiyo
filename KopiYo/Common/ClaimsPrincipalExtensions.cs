using System.Security.Claims;

namespace KopiYo.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id user yang sedang login. Melempar jika dipanggil di action yang tidak ter-authorize.</summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id)
            ? id
            : throw new InvalidOperationException("Claim NameIdentifier tidak ada atau bukan angka.");
    }

    public static string GetUsername(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public static string GetFullName(this ClaimsPrincipal user)
        => user.FindFirstValue(AppConstants.FullNameClaim) ?? user.GetUsername();

    public static bool IsAdmin(this ClaimsPrincipal user)
        => user.IsInRole(AppConstants.Roles.Admin);
}
