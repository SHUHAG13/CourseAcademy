using System.Globalization;

namespace CourseAcademy.Api.Common;

/// <summary>
/// Live session times are entered and shown in Bangladesh time and stored in UTC.
/// The wire format is "YYYY-MM-DDTHH:mm", the value of an HTML datetime-local input.
/// </summary>
public static class BangladeshTime
{
    public const string Format = "yyyy-MM-dd'T'HH:mm";

    private static readonly TimeZoneInfo Zone = Find();

    private static TimeZoneInfo Find()
    {
        foreach (var id in new[] { "Asia/Dhaka", "Bangladesh Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }
        // Bangladesh has no daylight saving time.
        return TimeZoneInfo.CreateCustomTimeZone("Asia/Dhaka", TimeSpan.FromHours(6), "Bangladesh", "Bangladesh");
    }

    public static bool TryParseToUtc(string? local, out DateTime utc)
    {
        utc = default;
        if (!DateTime.TryParseExact(local, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), Zone);
        return true;
    }

    public static string FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone).ToString(Format, CultureInfo.InvariantCulture);
}
