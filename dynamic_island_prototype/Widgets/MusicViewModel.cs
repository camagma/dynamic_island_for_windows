using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace dynamic_island_prototype.Widgets;

public sealed class MusicViewModel : INotifyPropertyChanged
{
    private string _title = "Nothing playing", _artist = "Start music in your favorite app", _status = "READY";
    private ImageSource? _artwork;
    private bool _canPrevious, _canNext, _canToggle, _playing;
    public string Title { get => _title; set => Set(ref _title, value); }
    public string Artist { get => _artist; set => Set(ref _artist, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public ImageSource? Artwork { get => _artwork; set => Set(ref _artwork, value); }
    public bool CanPrevious { get => _canPrevious; set => Set(ref _canPrevious, value); }
    public bool CanNext { get => _canNext; set => Set(ref _canNext, value); }
    public bool CanToggle { get => _canToggle; set => Set(ref _canToggle, value); }
    public bool Playing { get => _playing; set { Set(ref _playing, value); PropertyChanged?.Invoke(this, new(nameof(PlayGlyph))); } }
    public string PlayGlyph => Playing ? "\uE769" : "\uE768";
    public ICommand Previous { get; }
    public ICommand Next { get; }
    public ICommand Toggle { get; }
    public MusicViewModel(Func<string, Task> control)
    {
        Previous = new AsyncCommand(() => control("previous"));
        Next = new AsyncCommand(() => control("next"));
        Toggle = new AsyncCommand(() => control("toggle"));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
    }
    private sealed class AsyncCommand(Func<Task> execute) : ICommand
    {
        private bool _busy;
        public bool CanExecute(object? parameter) => !_busy;
        public event EventHandler? CanExecuteChanged;
        public async void Execute(object? parameter)
        {
            if (_busy) return;
            _busy = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try { await execute(); }
            finally { _busy = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
        }
    }
}
