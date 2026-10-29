using System.IO;
using System.Windows;
using System.Windows.Controls;
using dynamic_island_prototype.Services;

namespace dynamic_island_prototype.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsStore _store;
    private readonly CancellationTokenSource _lifetime = new();
    private CityResult? _selectedCity;
    private int _searchRevision;
    public SettingsWindow(SettingsStore store)
    {
        InitializeComponent();
        _store = store;
        CalendarAddress.Password = store.Current.CalendarUrl;
        CityQuery.Text = store.Current.City;
        CurrentCity.Text = string.IsNullOrEmpty(store.Current.City) ? "No city selected. Search and select a result." : "Current: " + store.Current.City;
        Closed += (_, _) => { _lifetime.Cancel(); _lifetime.Dispose(); };
    }
    private void CityQuery_Changed(object sender, TextChangedEventArgs e)
    {
        ++_searchRevision;
        _selectedCity = null;
        if (CityResults is not null) CityResults.ItemsSource = null;
    }
    private void CalendarAddress_Changed(object sender, RoutedEventArgs e)
    {
        if (CalendarPlaceholder is null) return;
        using var password = CalendarAddress.SecurePassword;
        CalendarPlaceholder.Visibility = password.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void CityResults_Changed(object sender, SelectionChangedEventArgs e)
    {
        _selectedCity = CityResults.SelectedItem as CityResult;
        if (_selectedCity is not null) CurrentCity.Text = "Selected: " + _selectedCity;
    }
    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        string query = CityQuery.Text.Trim();
        if (query.Length < 2) { Message.Text = "Enter at least two characters."; return; }
        int revision = ++_searchRevision;
        SearchButton.IsEnabled = false;
        Message.Text = "Searching…";
        var token = _lifetime.Token;
        try
        {
            var results = await WeatherService.SearchAsync(query, token);
            if (token.IsCancellationRequested || revision != _searchRevision) return;
            CityResults.ItemsSource = results;
            Message.Text = results.Count == 0 ? "No matching city. Try another name." : "Select your city from the list, then Save.";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception) { if (!token.IsCancellationRequested) Message.Text = "City search failed. Check your connection and try again."; }
        finally { if (!token.IsCancellationRequested) SearchButton.IsEnabled = true; }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string url = CalendarAddress.Password.Trim();
        if (url.Length > 0 && !CalendarService.IsValidUrl(url)) { Message.Text = "Calendar address must be an HTTPS iCal URL."; return; }
        if (_selectedCity is null && CityQuery.Text.Trim() != _store.Current.City)
        { Message.Text = "Search and select a city before saving."; return; }
        var settings = _store.Current with { CalendarUrl = url };
        if (_selectedCity is { } city)
            settings = settings with { City = city.Name + ", " + city.Country, Latitude = city.Latitude, Longitude = city.Longitude };
        try { _store.Save(settings); DialogResult = true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Message.Text = "Unable to save settings. Check access to your local app data folder."; }
    }
}

