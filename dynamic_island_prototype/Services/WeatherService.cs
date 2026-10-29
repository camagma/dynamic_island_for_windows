using System.Net.Http;
using System.Text.Json;
using System.Globalization;

namespace dynamic_island_prototype.Services;

public sealed record CityResult(string Name, string Country, string Region, double Latitude, double Longitude)
{
    public override string ToString() => $"{Name}, {Region}, {Country}";
}
public sealed record WeatherReading(double Temperature, string Icon, string Description);

public static class WeatherService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };
    public static async Task<IReadOnlyList<CityResult>> SearchAsync(string city, CancellationToken token)
    {
        var json = await Client.GetStringAsync($"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=8&language=en&format=json", token);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results)) return [];
        return results.EnumerateArray().Select(r => new CityResult(r.GetProperty("name").GetString()!,
            r.TryGetProperty("country", out var country) ? country.GetString() ?? "" : "",
            r.TryGetProperty("admin1", out var region) ? region.GetString() ?? "" : "",
            r.GetProperty("latitude").GetDouble(), r.GetProperty("longitude").GetDouble())).ToArray();
    }
    public static async Task<WeatherReading> ReadAsync(double latitude, double longitude, CancellationToken token)
    {
        string url = FormattableString.Invariant($"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code,is_day&temperature_unit=celsius");
        using var doc = JsonDocument.Parse(await Client.GetStringAsync(url, token));
        var current = doc.RootElement.GetProperty("current");
        var (icon, description) = Describe(current.GetProperty("weather_code").GetInt32(), current.GetProperty("is_day").GetInt32() == 1);
        return new(current.GetProperty("temperature_2m").GetDouble(), icon, description);
    }
    public static (string Icon, string Description) Describe(int code, bool day) => code switch
    {
        0 => (day ? "\u2600" : "\u263E", day ? "Clear sky" : "Clear night"),
        1 or 2 => ("\u26C5", "Partly cloudy"), 3 => ("\u2601", "Overcast"),
        45 or 48 => ("\u224B", "Fog"), 51 or 53 or 55 or 56 or 57 => ("\u2602", "Drizzle"),
        61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => ("\u2602", "Rain"),
        71 or 73 or 75 or 77 or 85 or 86 => ("\u2744", "Snow"),
        95 or 96 or 99 => ("\u03DF", "Thunderstorm"), _ => ("?", "Unknown conditions")
    };
}
