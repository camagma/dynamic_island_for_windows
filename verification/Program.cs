using dynamic_island_prototype.Services;

int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) { Console.Error.WriteLine("FAIL: " + description); Environment.Exit(1); }
    Console.WriteLine("PASS: " + description);
    checks++;
}
string Feed(params string[] events) => "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//DynamicIsland//Verification//EN\r\n" + string.Join("\r\n", events) + "\r\nEND:VCALENDAR";
string Event(string id, string properties) => $"BEGIN:VEVENT\r\nUID:{id}\r\nDTSTAMP:20261001T000000Z\r\n{properties}\r\nEND:VEVENT";
var now = new DateTime(2026, 10, 29, 12, 0, 0);
var feed = Feed(
 Event("past", "DTSTART:20261029T090000\r\nDTEND:20261029T100000\r\nSUMMARY:Past"),
 Event("ongoing", "DTSTART:20261029T113000\r\nDTEND:20261029T123000\r\nSUMMARY:Ongoing"),
 Event("next", "DTSTART:20261029T140000\r\nDTEND:20261029T150000\r\nSUMMARY:Next"),
 Event("tomorrow", "DTSTART:20261030T000000\r\nDTEND:20261030T010000\r\nSUMMARY:Tomorrow"),
 Event("all-day", "DTSTART;VALUE=DATE:20261029\r\nDTEND;VALUE=DATE:20261030\r\nSUMMARY:All day"),
 Event("cancelled", "DTSTART:20261029T160000\r\nDTEND:20261029T170000\r\nSTATUS:CANCELLED\r\nSUMMARY:Cancelled"));
var agenda = CalendarService.ParseToday(feed, now);

Check(agenda.Select(e => e.Title).SequenceEqual(new[] { "All day", "Ongoing", "Next" }), "Today only, ordered, excludes past and cancelled events");
Check(agenda[0].Time == "All day" && agenda[1].Time.StartsWith("Now"), "All-day and ongoing labels");
var recurring = Feed(Event("daily", "DTSTART:20261001T150000\r\nDTEND:20261001T160000\r\nRRULE:FREQ=DAILY\r\nSUMMARY:Daily"));
Check(CalendarService.ParseToday(recurring, now).Single().Title == "Daily", "Unbounded recurrence is bounded to today");
var excluded = Feed(Event("excluded", "DTSTART:20261001T150000\r\nDTEND:20261001T160000\r\nRRULE:FREQ=DAILY\r\nEXDATE:20261029T150000\r\nSUMMARY:Excluded"));
Check(CalendarService.ParseToday(excluded, now).Count == 0, "EXDATE removes the occurrence");
var moved = Feed(
 Event("moved", "DTSTART:20261001T150000\r\nDTEND:20261001T160000\r\nRRULE:FREQ=DAILY\r\nSUMMARY:Original"),
 Event("moved", "RECURRENCE-ID:20261029T150000\r\nDTSTART:20261029T170000\r\nDTEND:20261029T180000\r\nSUMMARY:Moved"));
var movedAgenda = CalendarService.ParseToday(moved, now);
Check(movedAgenda.Count == 1 && movedAgenda[0].Title == "Moved" && movedAgenda[0].Time == "17:00", "RECURRENCE-ID replaces rather than duplicates an occurrence");
var multi = Feed(Event("multi", "DTSTART:20261025T150000\r\nDTEND:20261030T180000\r\nSUMMARY:Conference"));
Check(CalendarService.ParseToday(multi, now).Single().Title == "Conference", "Multi-day ongoing event");
// Convert a local near-midnight event to UTC to test both date filtering and display conversion.
var local = DateTime.SpecifyKind(now.Date.AddHours(23.5), DateTimeKind.Local);
string Utc(DateTime value) => value.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
var zoned = Feed(Event("utc", $"DTSTART:{Utc(local)}\r\nDTEND:{Utc(local.AddHours(1))}\r\nSUMMARY:UTC event"));
Check(CalendarService.ParseToday(zoned, now).Single().Time == "23:30", "UTC event maps to local date and time");
var midnight = Feed(Event("midnight", "DTSTART;VALUE=DATE:20261028\r\nDTEND;VALUE=DATE:20261029\r\nSUMMARY:Yesterday"));
Check(CalendarService.ParseToday(midnight, now).Count == 0, "All-day exclusive end date");
Check(WeatherService.Describe(0, true).Icon != WeatherService.Describe(0, false).Icon, "Clear day/night icons");
Check(WeatherService.Describe(95, true).Description == "Thunderstorm" && WeatherService.Describe(999, true).Description == "Unknown conditions", "WMO condition mapping and fallback");
Check(CalendarService.IsValidUrl("https://calendar.google.com/test.ics") && !CalendarService.IsValidUrl("http://example.com") && !CalendarService.IsValidUrl("file:///private"), "Calendar URL validation");
if (args.Contains("--live"))
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    var cities = await WeatherService.SearchAsync("Kyiv", timeout.Token);
    Check(cities.Count > 0, "Live geocoding returns cities");
    var weather = await WeatherService.ReadAsync(cities[0].Latitude, cities[0].Longitude, timeout.Token);
    Check(double.IsFinite(weather.Temperature) && weather.Description != "Unknown conditions", "Live weather response parses");
}
Console.WriteLine($"{checks} checks passed.");


