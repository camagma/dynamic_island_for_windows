using System.Windows;

namespace dynamic_island_prototype.Widgets;

public interface IWidget : IDisposable
{
    string Id { get; }
    object ViewModel { get; }
    FrameworkElement CreateCompactView();
    FrameworkElement CreateExpandedView();
    Task InitializeAsync();
}
