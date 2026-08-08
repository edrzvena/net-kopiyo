namespace KopiYo.Services.Interfaces;

public interface IOrderNumberGenerator
{
    /// <summary>
    /// Menghasilkan nomor order berikutnya untuk tanggal bisnis tertentu.
    ///
    /// WAJIB dipanggil di dalam transaksi yang sudah terbuka: implementasinya
    /// mengambil row lock yang baru dilepas saat commit. Dipanggil di luar
    /// transaksi, lock-nya langsung lepas dan proteksi race-nya hilang.
    ///
    /// Tidak memanggil SaveChanges — pemanggil yang memiliki unit of work-nya.
    /// </summary>
    Task<string> NextAsync(DateOnly businessDate, CancellationToken ct);
}
