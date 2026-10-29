using System.Windows;
using System.Windows.Media.Animation;

namespace dynamic_island_prototype;

// WPF has no built-in continuous CornerRadius animation.
internal sealed class CornerRadiusAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty = DependencyProperty.Register(nameof(From), typeof(CornerRadius), typeof(CornerRadiusAnimation));
    public static readonly DependencyProperty ToProperty = DependencyProperty.Register(nameof(To), typeof(CornerRadius), typeof(CornerRadiusAnimation));
    public static readonly DependencyProperty EasingFunctionProperty = DependencyProperty.Register(nameof(EasingFunction), typeof(IEasingFunction), typeof(CornerRadiusAnimation));
    public CornerRadius From { get => (CornerRadius)GetValue(FromProperty); set => SetValue(FromProperty, value); }
    public CornerRadius To { get => (CornerRadius)GetValue(ToProperty); set => SetValue(ToProperty, value); }
    public IEasingFunction? EasingFunction { get => (IEasingFunction?)GetValue(EasingFunctionProperty); set => SetValue(EasingFunctionProperty, value); }
    public override Type TargetPropertyType => typeof(CornerRadius);
    public CornerRadiusAnimation() { }
    public CornerRadiusAnimation(CornerRadius from, CornerRadius to, Duration duration)
    { From = from; To = to; Duration = duration; }
    protected override Freezable CreateInstanceCore() => new CornerRadiusAnimation();
    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock clock)
    {
        double progress = clock.CurrentProgress ?? 0;
        progress = EasingFunction?.Ease(progress) ?? progress;
        double Lerp(double a, double b) => a + (b - a) * progress;
        return new CornerRadius(Lerp(From.TopLeft, To.TopLeft), Lerp(From.TopRight, To.TopRight),
            Lerp(From.BottomRight, To.BottomRight), Lerp(From.BottomLeft, To.BottomLeft));
    }
}
