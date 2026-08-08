namespace KopiYo.Data.Configurations;

internal static class SeedConstants
{
    /// <summary>
    /// Tanggal KONSTAN untuk semua data HasData.
    /// Jangan pernah pakai DateTime.Now di HasData — nilainya berubah tiap kali
    /// model dibangun, jadi setiap `dotnet ef migrations add` akan menghasilkan
    /// migration UpdateData yang sia-sia.
    /// </summary>
    public static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
}
