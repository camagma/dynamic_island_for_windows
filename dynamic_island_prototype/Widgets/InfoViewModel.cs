using System.ComponentModel;
using System.Runtime.CompilerServices;
using dynamic_island_prototype.Services;

namespace dynamic_island_prototype.Widgets;

public sealed class InfoViewModel : INotifyPropertyChanged
{
    private string _title = "Loading…", _subtitle = "", _icon = "", _status = "", _footer = "";
    private IReadOnlyList<AgendaEvent> _events = [];
    public string Title { get => _title; set => Set(ref _title, value); }
    public string Subtitle { get => _subtitle; set => Set(ref _subtitle, value); }
    public string Icon { get => _icon; set => Set(ref _icon, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public string Footer { get => _footer; set => Set(ref _footer, value); }
    public IReadOnlyList<AgendaEvent> Events { get => _events; set => Set(ref _events, value); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
    }
}

