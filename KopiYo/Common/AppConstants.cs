namespace KopiYo.Common;

public static class AppConstants
{
    /// <summary>
    /// Nama role sebagai const string supaya bisa dipakai di atribut:
    /// [Authorize(Roles = AppConstants.Roles.Admin)]. Atribut hanya menerima
    /// konstanta compile-time, jadi ini tidak bisa diganti dengan enum.
    /// Nilainya WAJIB sama persis dengan UserRole.ToString().
    /// </summary>
    public static class Roles
    {
        public const string Admin = nameof(UserRole.Admin);
        public const string Kasir = nameof(UserRole.Kasir);
    }

    /// <summary>Prefix nomor order: KY-20260808-0001</summary>
    public const string OrderPrefix = "KY";

    /// <summary>Nama claim tambahan untuk menampilkan nama lengkap di navbar.</summary>
    public const string FullNameClaim = "FullName";

    public const int DefaultPageSize = 20;

    /// <summary>
    /// Semua waktu bisnis dihitung dalam WIB, bukan UTC dan bukan waktu server.
    /// Penjualan jam 00:30 WIB masuk buku hari ini menurut WIB.
    /// </summary>
    public const string WibTimeZoneId = "SE Asia Standard Time";
}
