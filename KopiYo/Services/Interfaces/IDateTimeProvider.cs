namespace KopiYo.Services.Interfaces;

/// <summary>
/// Satu-satunya sumber waktu di aplikasi ini. Tidak ada service yang boleh
/// memanggil DateTime.Now langsung — selain tidak bisa di-unit-test, waktu server
/// belum tentu WIB kalau nanti aplikasinya di-deploy ke cloud.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Waktu sekarang dalam WIB.</summary>
    DateTime NowWib { get; }

    /// <summary>
    /// Tanggal bisnis hari ini menurut WIB. Penjualan jam 00:30 WIB masuk buku
    /// hari ini, bukan kemarin (yang akan terjadi kalau memakai UTC).
    /// </summary>
    DateOnly TodayWib { get; }
}
