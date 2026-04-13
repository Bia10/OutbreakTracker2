using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using OutbreakTracker2.Application.Views.Common;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;

public partial class InGameEnemyView : UserControl, IDisposable
{
    private Border? _glowBorder;
    private CancellationTokenSource? _glowCts;
    private InGameEnemyViewModel? _currentViewModel;
    private bool _disposed;

    public InGameEnemyView()
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

        DataContextChanged -= OnDataContextChanged;

        if (_currentViewModel is not null)
            _currentViewModel.GlowTriggered -= OnGlowTriggered;

        _glowCts?.Cancel();
        _glowCts?.Dispose();
        _glowCts = null;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel is not null)
            _currentViewModel.GlowTriggered -= OnGlowTriggered;

        _currentViewModel = DataContext as InGameEnemyViewModel;

        if (_currentViewModel is not null)
            _currentViewModel.GlowTriggered += OnGlowTriggered;
    }

    private void OnGlowTriggered(object? sender, GlowEventArgs e)
    {
        Color color = e.Color;
        // Capture color before the Post so the closure does not hold the event args alive.
        // Check _disposed inside the closure: the View may be disposed between the event
        // firing and the Render pass executing.
        Dispatcher.UIThread.Post(() =>
        {
            if (!_disposed)
                StartGlowAnimation(color);
        });
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
