using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using dynamic_island_prototype.Platform;
using dynamic_island_prototype.Widgets;
using dynamic_island_prototype.Settings;
using System.Windows.Media;

namespace dynamic_island_prototype;

public partial class MainWindow : Window
{
    private readonly WidgetManager _widgets = new();
    private IslandWindowBehavior? _behavior;
    private bool _expanded;
    private readonly SettingsStore _settings = new();
    private bool _switching, _closed;
    private int _direction = 1;
    private bool _revealed;
    public MainWindow()
    {
        InitializeComponent();
        _widgets.PropertyChanged += ActiveChanged;
        _widgets.Add(new MusicWidget());
        _widgets.Add(new CalendarWidget(_settings));
        _widgets.Add(new WeatherWidget(_settings));
        WidgetTabs.ItemsSource = _widgets.Widgets;
        WidgetTabs.SelectedItem = _widgets.ActiveWidget;
        SourceInitialized += (_, _) => _behavior = new(this, SetRevealed,
            () => Pill.ContextMenu?.IsOpen == true || OwnedWindows.Count > 0);
        Loaded += InitializeWidgets;
        Closed += (_, _) =>
        {
            _closed = true;
            _behavior?.Dispose();
            _widgets.PropertyChanged -= ActiveChanged;
            _widgets.Dispose();
        };
    }
    private async void InitializeWidgets(object sender, RoutedEventArgs e)
    {
        Loaded -= InitializeWidgets;
        await Task.WhenAll(_widgets.Widgets.Select(widget => widget.InitializeAsync()));
    }
    private void ActiveChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WidgetManager.ActiveWidget)) return;
        Compact.Content = _widgets.ActiveWidget?.CreateCompactView();
        WidgetContent.Content = _widgets.ActiveWidget?.CreateExpandedView();
        WidgetTabs.SelectedItem = _widgets.ActiveWidget;
        if (_expanded && WidgetContent.Content is FrameworkElement view)
        {
            var shift = new TranslateTransform();
            view.RenderTransform = shift;
            var duration = TimeSpan.FromMilliseconds(180);
            view.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration));
            shift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(18 * _direction, 0, duration)
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
    }
    private void SetRevealed(bool reveal)
    {
        _revealed = reveal;
        if (reveal)
        {
            Pill.Visibility = Visibility.Visible;
            Animate(true);
        }
        Pill.IsHitTestVisible = reveal;
        var duration = TimeSpan.FromMilliseconds(reveal ? 260 : 190);
        var fade = new DoubleAnimation(reveal ? 1 : 0, duration)
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        if (!reveal) fade.Completed += (_, _) =>
        {
            if (_revealed || _closed) return;
            Pill.Visibility = Visibility.Hidden;
            Animate(false);
        };
        Pill.BeginAnimation(OpacityProperty, fade);
        RevealOffset.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(reveal ? 0 : -16, duration)
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }
    private void Animate(bool expand)
    {
        if (expand == _expanded) return;
        _expanded = expand;
        Expanded.Visibility = Visibility.Visible;
        Expanded.IsHitTestVisible = expand;
        Compact.IsHitTestVisible = !expand;
        var storyboard = new Storyboard();
        void Add(DependencyObject target, DependencyProperty property, double value)
        {
            var animation = new DoubleAnimation(value, TimeSpan.FromMilliseconds(260))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(property));
            storyboard.Children.Add(animation);
        }
        Add(Pill, WidthProperty, expand ? 384 : 288);
        Add(Pill, HeightProperty, expand ? 176 : 52);
        Add(Compact, OpacityProperty, expand ? 0 : 1);
        Add(Expanded, OpacityProperty, expand ? 1 : 0);
        double bottomRadius = expand ? 24 : 26;
        var radius = new CornerRadiusAnimation(Pill.CornerRadius, new CornerRadius(0, 0, bottomRadius, bottomRadius), TimeSpan.FromMilliseconds(260))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(radius, Pill);
        Storyboard.SetTargetProperty(radius, new PropertyPath(System.Windows.Controls.Border.CornerRadiusProperty));
        storyboard.Children.Add(radius);
        storyboard.Begin(this, HandoffBehavior.SnapshotAndReplace);
    }
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
    private void Settings_Click(object sender, RoutedEventArgs e) => new SettingsWindow(_settings) { Owner = this }.ShowDialog();
    private void WidgetTab_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // ListBoxItem's default click handler requires Focus() to succeed.
        // This overlay intentionally disallows focus, so activate directly.
        e.Handled = true;
        if (sender is System.Windows.Controls.ListBoxItem { DataContext: IWidget widget } &&
            widget != _widgets.ActiveWidget)
            SwitchWidget(widget);
    }
    private void WidgetTabs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (WidgetTabs.SelectedItem is IWidget widget && widget != _widgets.ActiveWidget)
            SwitchWidget(widget);
    }
    private void SwitchWidget(IWidget widget)
    {
        if (_switching || !_expanded || _closed) return;
        _switching = true;
        _direction = _widgets.Widgets.ToList().IndexOf(widget) > _widgets.Widgets.ToList().IndexOf(_widgets.ActiveWidget!) ? 1 : -1;
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(100));
        fade.Completed += (_, _) =>
        {
            if (_closed) return;
            _widgets.Activate(widget.Id);
            _switching = false;
        };
        if (WidgetContent.Content is FrameworkElement oldView) oldView.BeginAnimation(OpacityProperty, fade);
        else { _widgets.Activate(widget.Id); _switching = false; }
    }
}
