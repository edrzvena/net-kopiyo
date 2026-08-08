using System.Globalization;

namespace KopiYo.Common;

public static class MoneyExtensions
{
    private static readonly CultureInfo Id = new("id-ID");

    /// <summary>
    /// PERINGATAN: jangan panggil ini di dalam .Select() sebuah query EF —
    /// tidak bisa diterjemahkan ke SQL dan EF Core akan melempar exception.
    /// Pakai hanya setelah data ter-materialisasi, atau langsung di file .cshtml.
    /// </summary>
    public static string ToRupiah(this decimal value)
        => "Rp " + value.ToString("#,##0", Id);

    public static string ToRupiah(this decimal? value)
        => value.HasValue ? value.Value.ToRupiah() : "-";

    /// <summary>Kuantitas bahan: tampilkan desimal hanya jika memang ada (18 g, bukan 18,000 g).</summary>
    public static string ToQty(this decimal value)
        => value.ToString("0.###", Id);

    /// <summary>
    /// Pembulatan uang ke rupiah utuh. Dipakai di SETIAP langkah perhitungan order
    /// dan hasilnya yang disimpan, supaya Subtotal - Diskon + Service + Pajak
    /// benar-benar sama dengan GrandTotal (tidak meleset 1 rupiah di struk).
    /// AwayFromZero, bukan ToEven (default .NET), karena itu yang diharapkan kasir.
    /// </summary>
    public static decimal RoundRupiah(this decimal value)
        => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
