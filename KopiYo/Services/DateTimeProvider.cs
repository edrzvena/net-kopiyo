using KopiYo.Common;
using KopiYo.Services.Interfaces;

namespace KopiYo.Services;

public sealed class DateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    private static readonly TimeZoneInfo Wib = ResolveWib();

    public DateTime NowWib =>
        TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, Wib);

    public DateOnly TodayWib => DateOnly.FromDateTime(NowWib);

    private static TimeZoneInfo ResolveWib()
    {
        // Id zona waktu beda antara Windows ("SE Asia Standard Time") dan Linux ("Asia/Jakarta").
        // .NET 8+ sebenarnya sudah bisa menerjemahkan keduanya, tapi fallback ini
        // membuat aplikasi tetap jalan kalau database zona waktunya tidak lengkap.
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(AppConstants.WibTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("WIB", TimeSpan.FromHours(7), "WIB", "WIB");
        }
    }
}
