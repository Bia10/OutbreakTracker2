using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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

    // Mouse presence tracking for the distance overlay
    private bool _isMouseOverCanvas;
    private Point _mouseCanvasPosition;

    public MapCanvasView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            _playersSubscription?.Dispose();

            if (DataContext is MapCanvasViewModel viewModel)
            {
                _playersSubscription = viewModel
                    .PlayersObservable.ObserveOn(SynchronizationContext.Current)
                    .Subscribe(players =>
                    {
                        _lastPlayers = players;
                        Redraw();
                    });
            }
        };

        GameMapCanvas.SizeChanged += (_, _) => Redraw();

        // Zoom via mouse wheel
        GameMapCanvas.PointerWheelChanged += OnPointerWheelChanged;

        // Pan via mouse drag
        GameMapCanvas.PointerPressed += OnPointerPressed;
        GameMapCanvas.PointerMoved += OnPointerMoved;
        GameMapCanvas.PointerReleased += OnPointerReleased;

        // Mouse presence tracking for the Euclidean-distance overlay
        GameMapCanvas.PointerEntered += (_, e) =>
        {
            _isMouseOverCanvas = true;
            _mouseCanvasPosition = e.GetPosition(GameMapCanvas);
            Redraw();
        };
        GameMapCanvas.PointerExited += (_, _) =>
        {
            _isMouseOverCanvas = false;
            Redraw();
        };
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
        Point pos = e.GetPosition(GameMapCanvas);
        _mouseCanvasPosition = pos;

        if (_isDragging)
        {
            _panX = _panXAtDragStart + (pos.X - _dragStart.X);
            _panY = _panYAtDragStart + (pos.Y - _dragStart.Y);
        }

        Redraw();
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

        if (_isMouseOverCanvas)
            DrawDistanceLines(_lastPlayers, baseScaleX, baseScaleY);

        if (ZoomLabel is not null)
            ZoomLabel.Text = $"{_zoomLevel * 100:F0}%";
    }

    // ── Coordinate helpers ───────────────────────────────────────────────────

    private (double cx, double cy) WorldToCanvas(float worldX, float worldY, double baseScaleX, double baseScaleY)
    {
        double cx = worldX * baseScaleX * _zoomLevel + _panX;
        double cy = worldY * baseScaleY * _zoomLevel + _panY;
        return (cx, cy);
    }

    private (float worldX, float worldY) CanvasToWorld(
        double canvasX,
        double canvasY,
        double baseScaleX,
        double baseScaleY
    )
    {
        float worldX = (float)((canvasX - _panX) / (baseScaleX * _zoomLevel));
        float worldY = (float)((canvasY - _panY) / (baseScaleY * _zoomLevel));
        return (worldX, worldY);
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
            Avalonia.Controls.Canvas.SetTop(coordLabel, cy + CircleRadius + 13);

            GameMapCanvas.Children.Add(circle);
            GameMapCanvas.Children.Add(nameLabel);
            GameMapCanvas.Children.Add(coordLabel);
        }
    }

    /// <summary>
    /// When the mouse is over the canvas, draw dashed lines from the cursor to every active
    /// player and annotate each line with the Euclidean distance in game-world units.
    /// </summary>
    private void DrawDistanceLines(DecodedInGamePlayer[]? players, double baseScaleX, double baseScaleY)
    {
        if (players is null)
            return;

        (float mouseWorldX, float mouseWorldY) = CanvasToWorld(
            _mouseCanvasPosition.X,
            _mouseCanvasPosition.Y,
            baseScaleX,
            baseScaleY
        );

        IBrush lineBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
        IBrush labelBackground = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0));

        foreach (DecodedInGamePlayer player in players)
        {
            if (!player.IsEnabled || !player.IsInGame)
                continue;

            (double cx, double cy) = WorldToCanvas(player.PositionX, player.PositionY, baseScaleX, baseScaleY);

            double dx = player.PositionX - mouseWorldX;
            double dy = player.PositionY - mouseWorldY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // Dashed line from mouse cursor to player dot
            Line line = new()
            {
                StartPoint = _mouseCanvasPosition,
                EndPoint = new Point(cx, cy),
                Stroke = lineBrush,
                StrokeThickness = 1,
                Opacity = 0.7,
            };
            line.StrokeDashArray = new AvaloniaList<double> { 4, 3 };

            // Distance label at the midpoint of the line
            double midX = (_mouseCanvasPosition.X + cx) / 2;
            double midY = (_mouseCanvasPosition.Y + cy) / 2;

            TextBlock distLabel = new()
            {
                Text = $"{dist:F0}",
                Foreground = Brushes.White,
                FontSize = 8,
                Background = labelBackground,
            };

            Avalonia.Controls.Canvas.SetLeft(distLabel, midX + 2);
            Avalonia.Controls.Canvas.SetTop(distLabel, midY - 8);

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
