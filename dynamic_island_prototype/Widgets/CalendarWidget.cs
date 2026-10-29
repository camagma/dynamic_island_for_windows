using dynamic_island_prototype.Services;
using dynamic_island_prototype.Settings;
using System.Globalization;

namespace dynamic_island_prototype.Widgets;

public sealed class CalendarWidget : RefreshingWidget
{
    public override string Id => "calendar";
    public CalendarWidget(SettingsStore settings) : base(settings, TimeSpan.FromMinutes(1)) => Model.Icon = "\u25A6";
    protected override async Task LoadAsync(CancellationToken token)
    {
        string url = Settings.Current.CalendarUrl;
        Model.Status = "TODAY · " + DateTime.Now.ToString("ddd, MMM d", CultureInfo.GetCultureInfo("en-US")).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(url))
        {
            Model.Title = "Connect your calendar";
            Model.Subtitle = "Right-click the island > Settings";
            Model.Footer = "Google Calendar · read-only iCal";
            Model.Events = [];
            return;
        }
        var events = await CalendarService.ReadAsync(url, token);
        token.ThrowIfCancellationRequested();
        Model.Events = events;
        Model.Title = events.FirstOrDefault()?.Title ?? "No more events today";
        Model.Subtitle = events.FirstOrDefault()?.Time ?? "Enjoy your free time";
        Model.Footer = "Google Calendar · updated " + DateTime.Now.ToString("HH:mm");
    }
}

