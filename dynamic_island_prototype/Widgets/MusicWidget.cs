using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace dynamic_island_prototype.Widgets;

public sealed class MusicWidget : IWidget
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private readonly MusicViewModel _model;
    private int _revision;
    private bool _disposed;
    public string Id => "music";
    public object ViewModel => _model;
    public MusicWidget() => _model = new(ControlAsync);
    public FrameworkElement CreateCompactView() => new MusicCompactView { DataContext = _model };
    public FrameworkElement CreateExpandedView() => new MusicExpandedView { DataContext = _model };

    public async Task InitializeAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_disposed) return;
            _manager = manager;
            manager.CurrentSessionChanged += CurrentChanged;
            RefreshSession();
        }
        catch (Exception ex) { Report(ex, "Media access unavailable"); }
    }
    private void CurrentChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        => Dispatch(RefreshSession);
    private void Dispatch(Action action)
    {
        if (!_disposed) Application.Current.Dispatcher.BeginInvoke(() => { if (!_disposed) action(); });
    }
    private void RefreshSession()
    {
        Detach();
        _session = _manager?.GetCurrentSession();
        if (_session is not null)
        {
            _session.MediaPropertiesChanged += MediaChanged;
            _session.PlaybackInfoChanged += PlaybackChanged;
        }
        RefreshPlayback();
        _ = RefreshMetadataAsync();
    }
    private void MediaChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        => Dispatch(() => { _ = RefreshMetadataAsync(); });
    private void PlaybackChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        => Dispatch(RefreshPlayback);
    private void RefreshPlayback()
    {
        try
        {
            var info = _session?.GetPlaybackInfo();
            _model.CanNext = info?.Controls.IsNextEnabled == true;
            _model.CanPrevious = info?.Controls.IsPreviousEnabled == true;
            _model.CanToggle = info?.Controls.IsPlayPauseToggleEnabled == true;
            _model.Playing = info?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            _model.Status = _session is null ? "READY" : _model.Playing ? "NOW PLAYING" : "PAUSED";
        }
        catch (Exception ex) { Report(ex, "Player unavailable"); }
    }
    private async Task RefreshMetadataAsync()
    {
        int revision = ++_revision;
        var session = _session;
        _model.Artwork = null;
        _model.Title = "Nothing playing";
        _model.Artist = "Start music in your favorite app";
        if (session is null) return;
        try
        {
            var properties = await session.TryGetMediaPropertiesAsync();
            if (_disposed || revision != _revision) return;
            _model.Title = string.IsNullOrWhiteSpace(properties.Title) ? "Unknown track" : properties.Title;
            _model.Artist = string.IsNullOrWhiteSpace(properties.Artist) ? "Unknown artist" : properties.Artist;
            if (properties.Thumbnail is null) return;
            using var stream = await properties.Thumbnail.OpenReadAsync();
            if (stream.Size > 16 * 1024 * 1024) return;
            using var reader = new DataReader(stream);
            await reader.LoadAsync((uint)stream.Size);
            byte[] bytes = new byte[(int)stream.Size];
            reader.ReadBytes(bytes);
            using var memory = new System.IO.MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 160;
            bitmap.StreamSource = memory;
            bitmap.EndInit();
            bitmap.Freeze();
            if (!_disposed && revision == _revision) _model.Artwork = bitmap;
        }
        catch (Exception ex) { if (!_disposed && revision == _revision) Report(ex, "Track info unavailable"); }
    }
    private async Task ControlAsync(string action)
    {
        var session = _session;
        if (session is null || _disposed) return;
        try
        {
            bool accepted = action switch
            {
                "previous" => await session.TrySkipPreviousAsync(),
                "next" => await session.TrySkipNextAsync(),
                _ => await session.TryTogglePlayPauseAsync()
            };
            if (!accepted) _model.Status = "COMMAND UNAVAILABLE";
        }
        catch (Exception ex) { Report(ex, "Playback control unavailable"); }
    }
    private void Report(Exception ex, string message)
    {
        Debug.WriteLine(ex);
        if (!_disposed) _model.Status = message.ToUpperInvariant();
    }
    private void Detach()
    {
        if (_session is null) return;
        _session.MediaPropertiesChanged -= MediaChanged;
        _session.PlaybackInfoChanged -= PlaybackChanged;
    }
    public void Dispose()
    {
        _disposed = true;
        ++_revision;
        if (_manager is not null) _manager.CurrentSessionChanged -= CurrentChanged;
        Detach();
    }
}
