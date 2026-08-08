namespace KopiYo.Models;

/// <summary>
/// Satu baris per tanggal bisnis, menyimpan nomor urut terakhir hari itu.
///
/// Kenapa tidak MAX(seq)+1 atau COUNT(*)+1: dua kasir yang checkout di detik yang
/// sama sama-sama membaca MAX = 7, lalu sama-sama menulis 0008. Tabel counter ini
/// dikunci dengan UPDLOCK+HOLDLOCK di dalam transaksi checkout, jadi checkout kedua
/// menunggu, bukan balapan.
/// </summary>
public class OrderCounter
{
    /// <summary>Primary key. DateOnly dipetakan native ke kolom SQL `date` (EF 8+).</summary>
    public DateOnly BusinessDate { get; set; }

    public int LastSequence { get; set; }

    /// <summary>Pengaman tambahan kalau lock di atas entah bagaimana terlewat.</summary>
    public byte[]? RowVersion { get; set; }
}
