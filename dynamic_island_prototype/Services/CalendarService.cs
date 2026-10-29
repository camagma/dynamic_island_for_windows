using System.Net.Http;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace dynamic_island_prototype.Services;

public sealed record AgendaEvent(string Title, string Time, string Location);

public static class CalendarService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20), MaxResponseContentBufferSize = 8 * 1024 * 1024 };
    public static bool IsValidUrl(string text) => Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme == "https";
    public static async Task<IReadOnlyList<AgendaEvent>> ReadAsync(string url, CancellationToken token)
    {
        if (!IsValidUrl(url)) throw new ArgumentException("Use an HTTPS iCal URL.");
        string ics = await Client.GetStringAsync(url, token);
        return await Task.Run(() => ParseToday(ics, DateTime.Now), token);
    }
    private static DateTime Local(CalDateTime date) => date.IsFloating || !date.HasTime ? date.Value : date.AsUtc.ToLocalTime();
    public static IReadOnlyList<AgendaEvent> ParseToday(string ics, DateTime now)
    {
        if (!ics.TrimStart('\uFEFF', ' ', '\r', '\n', '\t').StartsWith("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Expected an iCalendar feed.");
        var calendar = Calendar.Load(ics) ?? throw new FormatException("Invalid iCalendar.");
        var tomorrow = now.Date.AddDays(1);
        // A broad floating-time envelope covers UTC/TZID events on either side of local midnight.
        // The final filter below applies the precise local day and excludes finished events.
        return calendar.GetOccurrences(new CalDateTime(DateTime.SpecifyKind(now.Date.AddDays(-1), DateTimeKind.Unspecified)))
            .TakeWhileBefore(new CalDateTime(DateTime.SpecifyKind(tomorrow.AddDays(1), DateTimeKind.Unspecified)))
            .Where(o => o.Source is CalendarEvent e && !string.Equals(e.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            .Select(o => new { Event = (CalendarEvent)o.Source, Start = o.Period.StartTime, End = o.Period.EffectiveEndTime })
            .Where(o => Local(o.Start) < tomorrow && (o.End is null ? Local(o.Start) : Local(o.End)) > now)
            .OrderBy(o => Local(o.Start))
            .Take(30)
            .Select(o => new AgendaEvent(string.IsNullOrWhiteSpace(o.Event.Summary) ? "Untitled event" : o.Event.Summary,
                !o.Start.HasTime ? "All day" : Local(o.Start) <= now ? "Now · " + Local(o.Start).ToString("HH:mm") : Local(o.Start).ToString("HH:mm"),
                o.Event.Location ?? ""))
            .ToArray();
    }
}



