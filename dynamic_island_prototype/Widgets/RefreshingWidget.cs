using System.Windows;
using System.Windows.Threading;
using dynamic_island_prototype.Settings;

namespace dynamic_island_prototype.Widgets;

// Shared only by widgets that periodically fetch data; music keeps its event-based lifecycle.
public abstract class RefreshingWidget : IWidget
{
    protected readonly SettingsStore Settings;
    protected readonly InfoViewModel Model = new();
    private readonly DispatcherTimer _timer;
    private CancellationTokenSource? _refresh;
    private bool _disposed;
    protected RefreshingWidget(SettingsStore settings, TimeSpan interval)
    {
        Settings = settings;
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += Tick;
        settings.Changed += SettingsChanged;
    }
    public abstract string Id { get; }
    public object ViewModel => Model;
    public FrameworkElement CreateCompactView() => new InfoCompactView { DataContext = Model };
    public FrameworkElement CreateExpandedView() => new InfoExpandedView { DataContext = Model };
    public async Task InitializeAsync() { _timer.Start(); await RefreshAsync(); }
    private void Tick(object? sender, EventArgs args) { if (_refresh is null) _ = RefreshAsync(); }
    private void SettingsChanged(object? sender, EventArgs args) => _ = RefreshAsync();
    private async Task RefreshAsync()
    {
        if (_disposed) return;
        _refresh?.Cancel();
        using var source = new CancellationTokenSource();
        _refresh = source;
        try { await LoadAsync(source.Token); }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { }
        catch (Exception) // Never log private calendar URLs or feed contents.
        {
            if (!source.IsCancellationRequested && !_disposed)
            {
                Model.Title = "Unable to update";
                Model.Subtitle = "Check your connection and Settings";
                Model.Events = [];
                Model.Status = Id.ToUpperInvariant() + " · OFFLINE";
                Model.Footer = "Will retry automatically";
            }
        }
        finally { if (ReferenceEquals(_refresh, source)) _refresh = null; }
    }
    protected abstract Task LoadAsync(CancellationToken token);
    public void Dispose()
    {
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= Tick;
        Settings.Changed -= SettingsChanged;
        _refresh?.Cancel();
    }
}
