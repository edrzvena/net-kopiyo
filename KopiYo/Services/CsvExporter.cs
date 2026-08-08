using System.Text;
using KopiYo.Services.Interfaces;

namespace KopiYo.Services;

public sealed class CsvExporter : ICsvExporter
{
    /// <summary>
    /// Titik koma, bukan koma. Windows berbahasa Indonesia memakai koma sebagai
    /// pemisah desimal, sehingga pemisah daftar Excel-nya adalah titik koma.
    /// File ber-koma akan terbuka sebagai satu kolom gepeng berisi teks berantakan.
    /// </summary>
    private const string Delimiter = ";";

    public byte[] Export<T>(IEnumerable<T> rows, IReadOnlyList<CsvColumn<T>> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Delimiter, columns.Select(c => Escape(c.Header))));

        foreach (var row in rows)
            sb.AppendLine(string.Join(Delimiter, columns.Select(c => Escape(c.Value(row)))));

        // UTF-8 DENGAN BOM. Tanpa BOM, Excel menebak encoding-nya ANSI dan teks
        // Indonesia berubah jadi mojibake.
        //
        // JEBAKAN: encoderShouldEmitUTF8Identifier: true TIDAK membuat GetBytes()
        // menulis BOM — flag itu hanya memengaruhi GetPreamble(), yang dipakai
        // StreamWriter. Jadi preamble-nya harus ditempel sendiri di depan.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var preamble = encoding.GetPreamble();
        var content = encoding.GetBytes(sb.ToString());

        var result = new byte[preamble.Length + content.Length];
        preamble.CopyTo(result, 0);
        content.CopyTo(result, preamble.Length);
        return result;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var needsQuote = value.Contains(Delimiter) || value.Contains('"')
                                                  || value.Contains('\n') || value.Contains('\r');

        return needsQuote ? '"' + value.Replace("\"", "\"\"") + '"' : value;
    }
}
