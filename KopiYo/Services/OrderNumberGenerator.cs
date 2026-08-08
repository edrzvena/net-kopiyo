using KopiYo.Common;
using KopiYo.Data;
using KopiYo.Models;
using KopiYo.Services.Interfaces;

namespace KopiYo.Services;

public sealed class OrderNumberGenerator(AppDbContext db) : IOrderNumberGenerator
{
    public async Task<string> NextAsync(DateOnly businessDate, CancellationToken ct)
    {
        // UPDLOCK  : ambil update-lock saat MEMBACA, bukan saat menulis. Tanpa ini
        //            dua transaksi sama-sama membaca LastSequence = 7 lalu sama-sama
        //            menulis 8 — salah satunya kena pelanggaran unique index, atau
        //            (kalau index-nya tidak ada) kamu dapat dua order bernomor sama.
        // HOLDLOCK : mengunci RANGE kuncinya, bukan cuma baris yang ada. Ini yang
        //            membuat cabang "insert kalau baris tanggal ini belum ada" aman:
        //            transaksi kedua tidak bisa menyelipkan baris tanggal yang sama.
        //
        // Lock-nya bertahan sampai transaksi milik pemanggil di-commit, jadi checkout
        // yang bersamaan akan MENUNGGU di sini, bukan balapan.
        var counter = await db.OrderCounters
            .FromSql($"""
                      SELECT * FROM OrderCounters WITH (UPDLOCK, HOLDLOCK)
                      WHERE BusinessDate = {businessDate}
                      """)
            .FirstOrDefaultAsync(ct);

        if (counter is null)
        {
            counter = new OrderCounter { BusinessDate = businessDate, LastSequence = 0 };
            db.OrderCounters.Add(counter);
        }

        counter.LastSequence++;

        return $"{AppConstants.OrderPrefix}-{businessDate:yyyyMMdd}-{counter.LastSequence:D4}";
    }
}
