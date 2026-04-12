using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

// The coordinate system maps raw game-world coordinates onto the canvas with zoom/pan support.
// Self  = green  (player slot 0)
// Ally  = blue   (player slots 1-3, human characters)
// NPC   = yellow (player slots with non-zero NameId, i.e. friendly NPCs)
// Enemy rendering is not yet implemented — position offsets for the flat enemy list are unknown.
// TODO: Map background images are not yet available; background is deferred until a scenario map is loaded.
public partial class MapCanvasView : UserControl
{
    private const double CircleRadius = 5;
    private const double MaxMapCoordinate = 25000.0;
    private const double ZoomStep = 1.15;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 20.0;
    private const double LabelWidth = 80;
    private const double CoordLabelOffsetY = 13;
    private const double DistLabelOffsetX = 2;
    private const double DistLabelOffsetY = -8;

    private IDisposable? _playersSubscription;
    private DecodedInGamePlayer[]? _lastPlayers;

    // Zoom and pan state
    private double _zoomLevel = 1.0;
    private double _panX;
    private double _panY;
    private bool _isDragging;
    private Point _dragStart;
    private double _panXAtDragStart;
    private double _panYAtDragStart;

    // Ctrl-held ally-distance overlay state
    private bool _isMouseOverCanvas;
    private bool _isCtrlHeld;

    public MapCanvasView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        GameMapCanvas.SizeChanged += (_, _) => Redraw();

        // Zoom via mouse wheel
        GameMapCanvas.PointerWheelChanged += OnPointerWheelChanged;

        // Pan via mouse drag
        GameMapCanvas.PointerPressed += OnPointerPressed;
        GameMapCanvas.PointerMoved += OnPointerMoved;
        GameMapCanvas.PointerReleased += OnPointerReleased;

        // Track whether the mouse is over the canvas; focus it so Ctrl key events land here.
        GameMapCanvas.PointerEntered += (_, _) =>
        {
            _isMouseOverCanvas = true;
            GameMapCanvas.Focus();
        };
        GameMapCanvas.PointerExited += (_, _) =>
        {
            _isMouseOverCanvas = false;
            _isCtrlHeld = false;
            Redraw();
        };

        // Ctrl toggles the ally-distance overlay while the mouse is inside the canvas.
        GameMapCanvas.KeyDown += (_, e) =>
        {
            if (e.Key is Key.LeftCtrl or Key.RightCtrl)
            {
                _isCtrlHeld = true;
                Redraw();
            }
        };
        GameMapCanvas.KeyUp += (_, e) =>
        {
            if (e.Key is Key.LeftCtrl or Key.RightCtrl)
            {
                _isCtrlHeld = false;
                Redraw();
            }
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ResubscribeToPlayers();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _playersSubscription?.Dispose();
        _playersSubscription = null;
        _lastPlayers = null;
        base.OnDetachedFromVisualTree(e);
    }

    // Zoom via mouse wheel

    private void OnDataContextChanged(object? sender, EventArgs e) => ResubscribeToPlayers();

    private void ResubscribeToPlayers()
    {
        _playersSubscription?.Dispose();
        _playersSubscription = null;

        if (DataContext is not MapCanvasViewModel viewModel)
        {
            _lastPlayers = null;
            Redraw();
            return;
        }

        if (SynchronizationContext.Current is { } synchronizationContext)
        {
            _playersSubscription = viewModel
                .PlayersObservable.ObserveOn(synchronizationContext)
                .Subscribe(OnPlayersChanged);
            return;
        }

        _playersSubscription = viewModel.PlayersObservable.Subscribe(players =>
            Dispatcher.UIThread.Post(() => OnPlayersChanged(players))
        );
    }

    private void OnPlayersChanged(DecodedInGamePlayer[] players)
    {
        _lastPlayers = players;
        Redraw();
    }

    // ── Zoom / pan event handlers ────────────────────────────────────────────

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        Point mousePos = e.GetPosition(GameMapCanvas);

        double factor = e.Delta.Y > 0 ? ZoomStep : 1.0 / ZoomStep;
        double newZoom = Math.Clamp(_zoomLevel * factor, MinZoom, MaxZoom);

        // Keep the world point currently under the cursor fixed after zoom
        double ratio = newZoom / _zoomLevel;
        _panX = mousePos.X - (mousePos.X - _panX) * ratio;
        _panY = mousePos.Y - (mousePos.Y - _panY) * ratio;
        _zoomLevel = newZoom;

        Redraw();
        e.Handled = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint(GameMapCanvas);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        _dragStart = point.Position;
        _panXAtDragStart = _panX;
        _panYAtDragStart = _panY;
        GameMapCanvas.Cursor = new Cursor(StandardCursorType.Hand);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        // Keep Ctrl state in sync with whatever the pointer event reports.
        _isCtrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (_isDragging)
        {
            Point pos = e.GetPosition(GameMapCanvas);
            _panX = _panXAtDragStart + (pos.X - _dragStart.X);
            _panY = _panYAtDragStart + (pos.Y - _dragStart.Y);
            Redraw();
        }
        else if (_isCtrlHeld)
        {
            // Distance overlay state changed — redraw so lines appear / disappear promptly.
            Redraw();
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        GameMapCanvas.Cursor = Cursor.Default;
    }

    private void OnResetZoomClick(object? sender, RoutedEventArgs e)
    {
        _zoomLevel = 1.0;
        _panX = 0;
        _panY = 0;
        Redraw();
    }

    // ── Redraw ───────────────────────────────────────────────────────────────

    private void Redraw()
    {
        GameMapCanvas.Children.Clear();

        double canvasW =
            GameMapCanvas.Bounds.Width > 0 ? GameMapCanvas.Bounds.Width : ViewModel?.MapWidth ?? MaxMapCoordinate;
        double canvasH =
            GameMapCanvas.Bounds.Height > 0 ? GameMapCanvas.Bounds.Height : ViewModel?.MapHeight ?? MaxMapCoordinate;

        double baseScaleX = canvasW / MaxMapCoordinate;
        double baseScaleY = canvasH / MaxMapCoordinate;

        DrawPlayers(_lastPlayers, baseScaleX, baseScaleY);

        // Show ally distances when Ctrl is held and the pointer is inside the canvas.
        if (_isMouseOverCanvas && _isCtrlHeld)
            DrawAllyDistances(_lastPlayers, baseScaleX, baseScaleY);

        if (ZoomLabel is not null)
            ZoomLabel.Text = $"{_zoomLevel * 100:F0}%";
    }

    // ── Coordinate helper ────────────────────────────────────────────────────

    private (double cx, double cy) WorldToCanvas(float worldX, float worldY, double baseScaleX, double baseScaleY)
    {
        double cx = worldX * baseScaleX * _zoomLevel + _panX;
        double cy = worldY * baseScaleY * _zoomLevel + _panY;
        return (cx, cy);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    private void DrawPlayers(DecodedInGamePlayer[]? players, double baseScaleX, double baseScaleY)
    {
        if (players is null)
            return;

        bool anyInGame = false;
        foreach (DecodedInGamePlayer p in players)
        {
            if (p.IsEnabled && p.IsInGame)
            {
                anyInGame = true;
                break;
            }
        }

        if (!anyInGame)
            return;

        for (int i = 0; i < players.Length; i++)
        {
            DecodedInGamePlayer player = players[i];
            if (!player.IsEnabled || !player.IsInGame)
                continue;

            bool isSelf = i == 0;
            bool isFriendlyNpc = player.NameId != 0;

            IBrush fill =
                isSelf ? Brushes.Green
                : isFriendlyNpc ? Brushes.Yellow
                : Brushes.Blue;

            IBrush stroke =
                isSelf ? Brushes.DarkGreen
                : isFriendlyNpc ? Brushes.DarkGoldenrod
                : Brushes.DarkBlue;

            (double cx, double cy) = WorldToCanvas(player.PositionX, player.PositionY, baseScaleX, baseScaleY);

            Ellipse circle = new()
            {
                Width = CircleRadius * 2,
                Height = CircleRadius * 2,
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = 1,
            };

            Avalonia.Controls.Canvas.SetLeft(circle, cx - CircleRadius);
            Avalonia.Controls.Canvas.SetTop(circle, cy - CircleRadius);

            // Name label centered below the dot
            TextBlock nameLabel = new()
            {
                Text = player.Name,
                Foreground = Brushes.White,
                FontSize = 9,
                TextAlignment = TextAlignment.Center,
                Width = LabelWidth,
            };

            Avalonia.Controls.Canvas.SetLeft(nameLabel, cx - LabelWidth / 2);
            Avalonia.Controls.Canvas.SetTop(nameLabel, cy + CircleRadius + 2);

            // Coordinates in brackets in smaller font below the name
            TextBlock coordLabel = new()
            {
                Text = $"({player.PositionX:F0}, {player.PositionY:F0})",
                Foreground = Brushes.LightGray,
                FontSize = 7,
                TextAlignment = TextAlignment.Center,
                Width = LabelWidth,
            };

            Avalonia.Controls.Canvas.SetLeft(coordLabel, cx - LabelWidth / 2);
            Avalonia.Controls.Canvas.SetTop(coordLabel, cy + CircleRadius + CoordLabelOffsetY);

            GameMapCanvas.Children.Add(circle);
            GameMapCanvas.Children.Add(nameLabel);
            GameMapCanvas.Children.Add(coordLabel);
        }
    }

    /// <summary>
    /// Draws dashed lines from self (slot 0) to every other active ally, annotated with the
    /// Euclidean distance in game-world units. Shown while Ctrl is held and the mouse is inside the canvas.
    /// </summary>
    private void DrawAllyDistances(DecodedInGamePlayer[]? players, double baseScaleX, double baseScaleY)
    {
        if (players is null || players.Length < 2)
            return;

        DecodedInGamePlayer self = players[0];
        if (!self.IsEnabled || !self.IsInGame)
            return;

        (double selfCx, double selfCy) = WorldToCanvas(self.PositionX, self.PositionY, baseScaleX, baseScaleY);

        IBrush lineBrush = new SolidColorBrush(Color.FromArgb(200, 100, 220, 255));
        IBrush labelBackground = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0));

        for (int i = 1; i < players.Length; i++)
        {
            DecodedInGamePlayer ally = players[i];
            if (!ally.IsEnabled || !ally.IsInGame)
                continue;

            (double allyCx, double allyCy) = WorldToCanvas(ally.PositionX, ally.PositionY, baseScaleX, baseScaleY);

            double dx = ally.PositionX - self.PositionX;
            double dy = ally.PositionY - self.PositionY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // Dashed line from self to ally
            Line line = new()
            {
                StartPoint = new Point(selfCx, selfCy),
                EndPoint = new Point(allyCx, allyCy),
                Stroke = lineBrush,
                StrokeThickness = 1,
                Opacity = 0.8,
            };
            line.StrokeDashArray = new AvaloniaList<double> { 4, 3 };

            // Distance label at the midpoint of the line
            double midX = (selfCx + allyCx) / 2;
            double midY = (selfCy + allyCy) / 2;

            TextBlock distLabel = new()
            {
                Text = $"{dist:F0}",
                Foreground = Brushes.White,
                FontSize = 8,
                Background = labelBackground,
            };

            Avalonia.Controls.Canvas.SetLeft(distLabel, midX + DistLabelOffsetX);
            Avalonia.Controls.Canvas.SetTop(distLabel, midY + DistLabelOffsetY);

            GameMapCanvas.Children.Add(line);
            GameMapCanvas.Children.Add(distLabel);
        }
    }

    protected void OnUnloaded()
    {
        _playersSubscription?.Dispose();
        _playersSubscription = null;
        _lastPlayers = null;

        base.OnUnloaded(null!);
    }

    private MapCanvasViewModel? ViewModel => DataContext as MapCanvasViewModel;
}
