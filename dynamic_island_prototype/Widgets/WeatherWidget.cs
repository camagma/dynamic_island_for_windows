using dynamic_island_prototype.Services;
using dynamic_island_prototype.Settings;
using System.Globalization;

namespace dynamic_island_prototype.Widgets;

public sealed class WeatherWidget : RefreshingWidget
{
    public override string Id => "weather";
    public WeatherWidget(SettingsStore settings) : base(settings, TimeSpan.FromMinutes(15)) => Model.Icon = "\u2600";
    protected override async Task LoadAsync(CancellationToken token)
    {
        var settings = Settings.Current;
        Model.Status = "WEATHER";
        if (settings.Latitude is null || settings.Longitude is null)
        {
            Model.Title = "Choose your city";
            Model.Subtitle = "Right-click the island > Settings";
            Model.Footer = "Weather: Open-Meteo · Location: GeoNames";
            return;
        }
        var reading = await WeatherService.ReadAsync(settings.Latitude.Value, settings.Longitude.Value, token);
        token.ThrowIfCancellationRequested();
        Model.Icon = reading.Icon;
        Model.Title = reading.Temperature.ToString("0°'C'", CultureInfo.InvariantCulture) + " · " + settings.City;
        Model.Subtitle = reading.Description;
        Model.Status = "CURRENT WEATHER · " + DateTime.Now.ToString("HH:mm");
        Model.Footer = "Weather: Open-Meteo · Location: GeoNames";
    }
}

