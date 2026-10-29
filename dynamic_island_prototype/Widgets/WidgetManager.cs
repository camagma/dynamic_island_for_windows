using System.Collections.ObjectModel;
using System.ComponentModel;

namespace dynamic_island_prototype.Widgets;

public sealed class WidgetManager : INotifyPropertyChanged, IDisposable
{
    private readonly ObservableCollection<IWidget> _widgets = [];
    public ReadOnlyObservableCollection<IWidget> Widgets { get; }
    public IWidget? ActiveWidget { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public WidgetManager() => Widgets = new(_widgets);

    public void Add(IWidget widget)
    {
        if (_widgets.Any(w => w.Id == widget.Id))
            throw new ArgumentException("Widget ID must be unique.", nameof(widget));
        _widgets.Add(widget);
        if (ActiveWidget is null) Activate(widget.Id);
    }

    public void Activate(string id)
    {
        ActiveWidget = _widgets.First(w => w.Id == id);
        PropertyChanged?.Invoke(this, new(nameof(ActiveWidget)));
    }

    public void Move(int oldIndex, int newIndex) => _widgets.Move(oldIndex, newIndex);
    public void ActivateRelative(int offset)
    {
        if (_widgets.Count == 0) return;
        int index = ActiveWidget is null ? 0 : _widgets.IndexOf(ActiveWidget);
        Activate(_widgets[((index + offset) % _widgets.Count + _widgets.Count) % _widgets.Count].Id);
    }
    public void Dispose()
    {
        foreach (var widget in _widgets) widget.Dispose();
    }
}
