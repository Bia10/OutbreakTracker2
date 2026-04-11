using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

public partial class ScenarioItemSlotView : UserControl, IDisposable
{
    private Border? _glowBorder;
    private CancellationTokenSource? _glowCts;
    private ScenarioItemSlotViewModel? _currentViewModel;
    private bool _disposed;

    public ScenarioItemSlotView()
    {
        InitializeComponent();
        _glowBorder = this.FindControl<Border>("GlowBorder");
        DataContextChanged += OnDataContextChanged;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_currentViewModel is not null)
            _currentViewModel.GlowTriggered -= OnGlowTriggered;

        _glowCts?.Cancel();
        _glowCts?.Dispose();
        _glowCts = null;
    }

    private void OnDetailsMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (ItemCard is not null)
            FlyoutBase.ShowAttachedFlyout(ItemCard);
    }

    private void OnDetailsFlyoutClose(object? sender, RoutedEventArgs e)
    {
        if (ItemCard?.GetValue(FlyoutBase.AttachedFlyoutProperty) is FlyoutBase flyout)
            flyout.Hide();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel is not null)
            _currentViewModel.GlowTriggered -= OnGlowTriggered;

        _currentViewModel = DataContext as ScenarioItemSlotViewModel;

        if (_currentViewModel is not null)
            _currentViewModel.GlowTriggered += OnGlowTriggered;
    }

    private void OnGlowTriggered(object? sender, GlowEventArgs e)
    {
        Dispatcher.UIThread.Post(() => StartGlowAnimation(e.Color));
    }

    private void StartGlowAnimation(Color color)
    {
        _glowCts?.Cancel();
        _glowCts?.Dispose();
        _glowCts = new CancellationTokenSource();

        if (_glowBorder is null)
            return;

        _glowBorder.BorderBrush = new SolidColorBrush(color);
        _glowBorder.Opacity = 1.0;

        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1.5),
            FillMode = FillMode.Forward,
            Easing = new ExponentialEaseOut(),
            Children =
            {
                new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(OpacityProperty, 1.0) } },
                new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(OpacityProperty, 0.0) } },
            },
        };

        _ = animation.RunAsync(_glowBorder, _glowCts.Token);
    }
}
