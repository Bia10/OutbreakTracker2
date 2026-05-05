using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

// The canvas projects live entity coordinates onto the currently visible scenario section.
public partial class MapCanvasView : UserControl
{
    private const double CircleRadius = 5;
    private const double EnemyCircleRadius = 4;
    private const double ItemBadgeBorderThickness = 1.5;
    private const double CalibrationOffsetStep = 0.005;
    private const double FineCalibrationOffsetStep = 0.001;
    private const double CalibrationScaleMultiplier = 1.05;
    private const double FineCalibrationScaleMultiplier = 1.01;
    private const double ZoomStep = 1.15;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 20.0;
    private const double LabelWidth = 80;
    private const double CoordLabelOffsetY = 13;
    private const double DistLabelOffsetX = 2;
    private const double DistLabelOffsetY = -8;

    private IDisposable? _enemiesSubscription;
    private IDisposable? _playersSubscription;
    private MapCanvasViewModel? _observedViewModel;
    private INotifyCollectionChanged? _observedScenarioItemsCollection;
    private readonly List<ScenarioItemSlotViewModel> _observedScenarioItems = [];
    private IReadOnlyList<ScenarioMapItemPlacement> _cachedScenarioItemPlacements = [];
    private MapSectionGeometry? _cachedScenarioItemPlacementSection;
    private int _cachedScenarioItemPlacementVersion = -1;
    private CancellationTokenSource? _scenarioItemPlacementCts;
    private MapSectionGeometry? _scenarioItemPlacementPendingSection;
    private int _scenarioItemPlacementPendingVersion = -1;
    private int _scenarioItemPlacementVersion;
    private DecodedEnemy[]? _lastEnemies;
    private DecodedInGamePlayer[]? _lastPlayers;
    private bool _isCalibrationMode;

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
        GameMapCanvas.KeyDown += OnCanvasKeyDown;
        GameMapCanvas.KeyUp += OnCanvasKeyUp;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ResubscribeToData();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachViewModel();
        DetachScenarioItemSubscriptions();
        _enemiesSubscription?.Dispose();
        _enemiesSubscription = null;
        _playersSubscription?.Dispose();
        _playersSubscription = null;
        CancelScenarioItemPlacementRefresh();
        ClearScenarioItemPlacementCache();
        _lastEnemies = null;
        _lastPlayers = null;
        base.OnDetachedFromVisualTree(e);
    }

    // Zoom via mouse wheel

    private void OnDataContextChanged(object? sender, EventArgs e) => ResubscribeToData();

    private void ResubscribeToData()
    {
        DetachViewModel();
        _enemiesSubscription?.Dispose();
        _enemiesSubscription = null;
        _playersSubscription?.Dispose();
        _playersSubscription = null;

        if (DataContext is not MapCanvasViewModel viewModel)
        {
            CancelScenarioItemPlacementRefresh();
            ClearScenarioItemPlacementCache();
            _lastEnemies = null;
            _lastPlayers = null;
            Redraw();
            return;
        }

        _observedViewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        AttachScenarioItemSubscriptions(viewModel);

        if (SynchronizationContext.Current is { } synchronizationContext)
        {
            _playersSubscription = viewModel
                .PlayersObservable.ObserveOn(synchronizationContext)
                .Subscribe(OnPlayersChanged);

            _enemiesSubscription = viewModel
                .EnemiesObservable.ObserveOn(synchronizationContext)
                .Subscribe(OnEnemiesChanged);

            return;
        }

        _playersSubscription = viewModel.PlayersObservable.Subscribe(players =>
            Dispatcher.UIThread.Post(() => OnPlayersChanged(players))
        );

        _enemiesSubscription = viewModel.EnemiesObservable.Subscribe(enemies =>
            Dispatcher.UIThread.Post(() => OnEnemiesChanged(enemies))
        );
    }

    private void OnPlayersChanged(DecodedInGamePlayer[] players)
    {
        _lastPlayers = players;
        Redraw();
    }

    private void OnEnemiesChanged(DecodedEnemy[] enemies)
    {
        _lastEnemies = enemies;
        Redraw();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName
            is nameof(MapCanvasViewModel.MapBackgroundImage)
                or nameof(MapCanvasViewModel.MapProjectionCalibration)
                or nameof(MapCanvasViewModel.MapBackgroundRelativePath)
                or nameof(MapCanvasViewModel.ProjectAllScenarioItemsOntoMap)
        )
        {
            if (e.PropertyName is nameof(MapCanvasViewModel.ProjectAllScenarioItemsOntoMap))
                InvalidateScenarioItemPlacements();

            Redraw();
        }
    }

    private void AttachScenarioItemSubscriptions(MapCanvasViewModel viewModel)
    {
        if (viewModel.ScenarioItemsViewModel.Items is INotifyCollectionChanged collection)
        {
            _observedScenarioItemsCollection = collection;
            _observedScenarioItemsCollection.CollectionChanged += OnScenarioItemsCollectionChanged;
        }

        RefreshScenarioItemSubscriptions(viewModel);
    }

    private void RefreshScenarioItemSubscriptions(MapCanvasViewModel viewModel)
    {
        foreach (ScenarioItemSlotViewModel item in _observedScenarioItems)
        {
            item.PropertyChanged -= OnScenarioItemPropertyChanged;
            item.ItemImageViewModel.PropertyChanged -= OnScenarioItemImagePropertyChanged;
        }

        _observedScenarioItems.Clear();

        foreach (ScenarioItemSlotViewModel item in viewModel.ScenarioItemsViewModel.Items)
        {
            _observedScenarioItems.Add(item);
            item.PropertyChanged += OnScenarioItemPropertyChanged;
            item.ItemImageViewModel.PropertyChanged += OnScenarioItemImagePropertyChanged;
        }
    }

    private void DetachScenarioItemSubscriptions()
    {
        if (_observedScenarioItemsCollection is not null)
        {
            _observedScenarioItemsCollection.CollectionChanged -= OnScenarioItemsCollectionChanged;
            _observedScenarioItemsCollection = null;
        }

        foreach (ScenarioItemSlotViewModel item in _observedScenarioItems)
        {
            item.PropertyChanged -= OnScenarioItemPropertyChanged;
            item.ItemImageViewModel.PropertyChanged -= OnScenarioItemImagePropertyChanged;
        }

        _observedScenarioItems.Clear();
    }

    private void OnScenarioItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_observedViewModel is null)
            return;

        RefreshScenarioItemSubscriptions(_observedViewModel);
        InvalidateScenarioItemPlacements();
        Redraw();
    }

    private void OnScenarioItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName
            is nameof(ScenarioItemSlotViewModel.IsProjectedOnMap)
                or nameof(ScenarioItemSlotViewModel.IsEmpty)
                or nameof(ScenarioItemSlotViewModel.IsHeldByPlayer)
                or nameof(ScenarioItemSlotViewModel.RoomId)
                or nameof(ScenarioItemSlotViewModel.RoomName)
        )
        {
            InvalidateScenarioItemPlacements();
            Redraw();
            return;
        }

        if (
            e.PropertyName
            is nameof(ScenarioItemSlotViewModel.Quantity)
                or nameof(ScenarioItemSlotViewModel.DisplayName)
        )
            Redraw();
    }

    private void OnScenarioItemImagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(ItemImageViewModel.ItemImage), StringComparison.Ordinal))
            Redraw();
    }

    private void DetachViewModel()
    {
        if (_observedViewModel is null)
            return;

        DetachScenarioItemSubscriptions();
        CancelScenarioItemPlacementRefresh();
        ClearScenarioItemPlacementCache();
        _observedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _observedViewModel = null;
    }

    private void InvalidateScenarioItemPlacements()
    {
        _scenarioItemPlacementVersion++;
        CancelScenarioItemPlacementRefresh();
    }

    private void CancelScenarioItemPlacementRefresh()
    {
        CancellationTokenSource? cts = _scenarioItemPlacementCts;
        _scenarioItemPlacementCts = null;
        _scenarioItemPlacementPendingSection = null;
        _scenarioItemPlacementPendingVersion = -1;

        if (cts is null)
            return;

        cts.Cancel();
        cts.Dispose();
    }

    private void ClearScenarioItemPlacementCache()
    {
        _cachedScenarioItemPlacements = [];
        _cachedScenarioItemPlacementSection = null;
        _cachedScenarioItemPlacementVersion = -1;
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

    private void OnToggleCalibrationClick(object? sender, RoutedEventArgs e)
    {
        _isCalibrationMode = !_isCalibrationMode;
        GameMapCanvas.Focus();
        UpdateOverlayLabels();
    }

    private void OnCanvasKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl or Key.RightCtrl)
        {
            _isCtrlHeld = true;
            Redraw();
        }

        if (!_isCalibrationMode || ViewModel is null)
            return;

        double offsetStep = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            ? FineCalibrationOffsetStep
            : CalibrationOffsetStep;
        double scaleMultiplier = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            ? FineCalibrationScaleMultiplier
            : CalibrationScaleMultiplier;

        switch (e.Key)
        {
            case Key.Left:
                ViewModel.AdjustProjectionOffset(-offsetStep, 0);
                break;
            case Key.Right:
                ViewModel.AdjustProjectionOffset(offsetStep, 0);
                break;
            case Key.Up:
                ViewModel.AdjustProjectionOffset(0, -offsetStep);
                break;
            case Key.Down:
                ViewModel.AdjustProjectionOffset(0, offsetStep);
                break;
            case Key.Add:
            case Key.OemPlus:
                ViewModel.AdjustProjectionScale(scaleMultiplier);
                break;
            case Key.Subtract:
            case Key.OemMinus:
                ViewModel.AdjustProjectionScale(1.0 / scaleMultiplier);
                break;
            case Key.D0:
            case Key.NumPad0:
                ViewModel.ResetProjectionCalibration();
                break;
            case Key.Escape:
                _isCalibrationMode = false;
                break;
            default:
                return;
        }

        Redraw();
        e.Handled = true;
    }

    private void OnCanvasKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl or Key.RightCtrl)
        {
            _isCtrlHeld = false;
            Redraw();
        }
    }

    // ── Redraw ───────────────────────────────────────────────────────────────

    private void Redraw()
    {
        GameMapCanvas.Children.Clear();

        double canvasW = GameMapCanvas.Bounds.Width > 0 ? GameMapCanvas.Bounds.Width : ViewModel?.MapWidth ?? 800;
        double canvasH = GameMapCanvas.Bounds.Height > 0 ? GameMapCanvas.Bounds.Height : ViewModel?.MapHeight ?? 600;

        MapSectionGeometry? activeSection = ResolveActiveSectionGeometry();

        DrawScenarioItems(activeSection, canvasW, canvasH);
        DrawEnemies(_lastEnemies, canvasW, canvasH);
        DrawPlayers(_lastPlayers, canvasW, canvasH);

        // Show ally distances when Ctrl is held and the pointer is inside the canvas.
        if (_isMouseOverCanvas && _isCtrlHeld)
            DrawAllyDistances(_lastPlayers, canvasW, canvasH);

        UpdateOverlayLabels();
    }

    // ── Coordinate helper ────────────────────────────────────────────────────

    private bool TryWorldToCanvas(
        float worldX,
        float worldY,
        short roomId,
        string roomName,
        double canvasW,
        double canvasH,
        out double cx,
        out double cy
    )
    {
        MapCanvasViewModel? viewModel = ViewModel;
        MapProjectionCalibration calibration = viewModel?.MapProjectionCalibration ?? MapProjectionCalibration.Default;

        if (
            !ScenarioMapProjectionResolver.TryProjectNormalizedPosition(
                viewModel?.ScenarioName ?? string.Empty,
                viewModel?.MapBackgroundRelativePath,
                viewModel?.RoomId ?? (short)-1,
                viewModel?.RoomName ?? string.Empty,
                roomId,
                roomName,
                worldX,
                worldY,
                calibration,
                out double normalizedX,
                out double normalizedY
            )
        )
        {
            cx = 0;
            cy = 0;
            return false;
        }

        cx = normalizedX * canvasW * _zoomLevel + _panX;
        cy = normalizedY * canvasH * _zoomLevel + _panY;
        return true;
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    private MapSectionGeometry? ResolveActiveSectionGeometry()
    {
        MapCanvasViewModel? viewModel = ViewModel;
        if (viewModel is null)
            return null;

        return ScenarioMapGeometryRenderer.TryResolveGeometry(
            viewModel.ScenarioName,
            viewModel.MapBackgroundRelativePath,
            viewModel.RoomId,
            viewModel.RoomName,
            out MapSectionGeometry? section,
            out _
        )
            ? section
            : null;
    }

    private void DrawScenarioItems(MapSectionGeometry? section, double canvasW, double canvasH)
    {
        MapCanvasViewModel? viewModel = ViewModel;
        if (viewModel is null || section is null)
        {
            CancelScenarioItemPlacementRefresh();
            return;
        }

        IReadOnlyList<ScenarioItemSlotViewModel> projectedItems = viewModel.GetProjectedScenarioItems();
        if (projectedItems.Count == 0)
        {
            CancelScenarioItemPlacementRefresh();
            _cachedScenarioItemPlacements = [];
            _cachedScenarioItemPlacementSection = section;
            _cachedScenarioItemPlacementVersion = _scenarioItemPlacementVersion;
            return;
        }

        int placementVersion = _scenarioItemPlacementVersion;
        if (!HasScenarioItemPlacementCache(section, placementVersion))
            EnsureScenarioItemPlacementsAsync(viewModel, section, placementVersion);

        if (
            !TryGetScenarioItemPlacements(
                section,
                placementVersion,
                out IReadOnlyList<ScenarioMapItemPlacement> placements
            )
        )
            return;

        double radiusScale = Math.Min(canvasW / section.Width, canvasH / section.Height) * _zoomLevel;

        foreach (ScenarioMapItemPlacement placement in placements)
        {
            double cx = (placement.CenterX / section.Width) * canvasW * _zoomLevel + _panX;
            double cy = (placement.CenterY / section.Height) * canvasH * _zoomLevel + _panY;
            double radius = placement.Radius * radiusScale;

            Control badge = CreateScenarioItemBadge(placement.Item, radius, viewModel.ProjectAllScenarioItemsOntoMap);
            Avalonia.Controls.Canvas.SetLeft(badge, cx - radius);
            Avalonia.Controls.Canvas.SetTop(badge, cy - radius);
            GameMapCanvas.Children.Add(badge);
        }
    }

    private bool HasScenarioItemPlacementCache(MapSectionGeometry section, int placementVersion) =>
        ReferenceEquals(_cachedScenarioItemPlacementSection, section)
        && _cachedScenarioItemPlacementVersion == placementVersion;

    private bool TryGetScenarioItemPlacements(
        MapSectionGeometry section,
        int placementVersion,
        out IReadOnlyList<ScenarioMapItemPlacement> placements
    )
    {
        if (HasScenarioItemPlacementCache(section, placementVersion))
        {
            placements = _cachedScenarioItemPlacements;
            return true;
        }

        if (ReferenceEquals(_cachedScenarioItemPlacementSection, section))
        {
            placements = _cachedScenarioItemPlacements;
            return true;
        }

        placements = [];
        return false;
    }

    private void EnsureScenarioItemPlacementsAsync(
        MapCanvasViewModel viewModel,
        MapSectionGeometry section,
        int placementVersion
    )
    {
        if (
            ReferenceEquals(_scenarioItemPlacementPendingSection, section)
            && _scenarioItemPlacementPendingVersion == placementVersion
        )
        {
            return;
        }

        CancelScenarioItemPlacementRefresh();

        CancellationTokenSource cts = new();
        _scenarioItemPlacementCts = cts;
        _scenarioItemPlacementPendingSection = section;
        _scenarioItemPlacementPendingVersion = placementVersion;
        _ = RefreshScenarioItemPlacementsAsync(viewModel, section, placementVersion, cts.Token);
    }

    private async Task RefreshScenarioItemPlacementsAsync(
        MapCanvasViewModel viewModel,
        MapSectionGeometry section,
        int placementVersion,
        CancellationToken cancellationToken
    )
    {
        try
        {
            IReadOnlyList<ScenarioMapItemPlacement> placements = await viewModel
                .GetProjectedScenarioItemPlacementsAsync(section, cancellationToken)
                .ConfigureAwait(false);

            Dispatcher.UIThread.Post(() =>
                ApplyScenarioItemPlacements(viewModel, section, placementVersion, placements)
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => ClearPendingScenarioItemPlacements(section, placementVersion));
            Trace.TraceError($"Failed to refresh scenario item placements: {ex}");
        }
    }

    private void ClearPendingScenarioItemPlacements(MapSectionGeometry section, int placementVersion)
    {
        if (
            !ReferenceEquals(_scenarioItemPlacementPendingSection, section)
            || _scenarioItemPlacementPendingVersion != placementVersion
        )
        {
            return;
        }

        CancellationTokenSource? cts = _scenarioItemPlacementCts;
        _scenarioItemPlacementPendingSection = null;
        _scenarioItemPlacementPendingVersion = -1;
        _scenarioItemPlacementCts = null;
        cts?.Dispose();
    }

    private void ApplyScenarioItemPlacements(
        MapCanvasViewModel viewModel,
        MapSectionGeometry section,
        int placementVersion,
        IReadOnlyList<ScenarioMapItemPlacement> placements
    )
    {
        if (!ReferenceEquals(_observedViewModel, viewModel))
            return;

        if (
            !ReferenceEquals(_scenarioItemPlacementPendingSection, section)
            || _scenarioItemPlacementPendingVersion != placementVersion
        )
        {
            return;
        }

        _cachedScenarioItemPlacements = placements;
        _cachedScenarioItemPlacementSection = section;
        _cachedScenarioItemPlacementVersion = placementVersion;
        ClearPendingScenarioItemPlacements(section, placementVersion);
        Redraw();
    }

    private static Control CreateScenarioItemBadge(
        ScenarioItemSlotViewModel item,
        double radius,
        bool projectingAllItems
    )
    {
        double diameter = radius * 2.0;
        Color outlineColor =
            !projectingAllItems && item.IsProjectedOnMap ? Color.FromRgb(255, 196, 64) : Color.FromRgb(230, 230, 230);

        Grid root = new()
        {
            Width = diameter,
            Height = diameter,
            IsHitTestVisible = false,
        };

        Ellipse background = new()
        {
            Width = diameter,
            Height = diameter,
            Fill = new SolidColorBrush(Color.FromArgb(220, 18, 18, 18)),
        };

        root.Children.Add(background);

        if (item.ItemImageViewModel.ItemImage is { } sprite)
        {
            Image image = new()
            {
                Width = diameter,
                Height = diameter,
                Source = sprite,
                Stretch = Stretch.UniformToFill,
                Clip = new EllipseGeometry(new Rect(0, 0, diameter, diameter)),
            };

            root.Children.Add(image);
        }
        else
        {
            TextBlock fallback = new()
            {
                Text = GetItemBadgeText(item.DisplayName),
                Foreground = Brushes.White,
                FontSize = Math.Max(6.0, diameter * 0.26),
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                Width = diameter,
                Height = diameter,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };

            root.Children.Add(fallback);
        }

        Ellipse outline = new()
        {
            Width = diameter,
            Height = diameter,
            Stroke = new SolidColorBrush(outlineColor),
            StrokeThickness = Math.Max(1.0, ItemBadgeBorderThickness * (diameter / 18.0)),
        };

        root.Children.Add(outline);

        if (item.Quantity > 1 && diameter >= 10.0)
        {
            Border badge = new()
            {
                Padding = new Thickness(2, 0),
                Background = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)),
                CornerRadius = new CornerRadius(999),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, -2, -2),
                Child = new TextBlock
                {
                    Text = item.QuantityText,
                    Foreground = Brushes.White,
                    FontSize = Math.Max(6.0, diameter * 0.2),
                    FontWeight = FontWeight.Bold,
                },
            };

            root.Children.Add(badge);
        }

        return root;
    }

    private static string GetItemBadgeText(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "?";

        string[] parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return displayName[..1].ToUpperInvariant();

        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[1][0]));
    }

    private void DrawEnemies(DecodedEnemy[]? enemies, double canvasW, double canvasH)
    {
        if (enemies is null)
            return;

        foreach (DecodedEnemy enemy in enemies)
        {
            if (!IsEnemyBasicallyValid(enemy))
                continue;

            if (
                !TryWorldToCanvas(
                    enemy.PositionX,
                    enemy.PositionY,
                    enemy.RoomId,
                    string.Empty,
                    canvasW,
                    canvasH,
                    out double cx,
                    out double cy
                )
            )
                continue;

            Ellipse circle = new()
            {
                Width = EnemyCircleRadius * 2,
                Height = EnemyCircleRadius * 2,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1,
            };

            Avalonia.Controls.Canvas.SetLeft(circle, cx - EnemyCircleRadius);
            Avalonia.Controls.Canvas.SetTop(circle, cy - EnemyCircleRadius);
            GameMapCanvas.Children.Add(circle);
        }
    }

    private void DrawPlayers(DecodedInGamePlayer[]? players, double canvasW, double canvasH)
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

            bool isSelf = ViewModel?.LocalPlayerSlotIndex == i;
            bool isFriendlyNpc = player.NameId != 0;

            IBrush fill = isFriendlyNpc ? Brushes.Yellow : Brushes.DodgerBlue;

            IBrush stroke =
                isFriendlyNpc ? Brushes.DarkGoldenrod
                : isSelf ? Brushes.White
                : Brushes.DarkBlue;

            if (
                !TryWorldToCanvas(
                    player.PositionX,
                    player.PositionY,
                    player.RoomId,
                    player.RoomName,
                    canvasW,
                    canvasH,
                    out double cx,
                    out double cy
                )
            )
                continue;

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
    private void DrawAllyDistances(DecodedInGamePlayer[]? players, double canvasW, double canvasH)
    {
        if (players is null || players.Length < 2)
            return;

        int selfIndex = ViewModel?.LocalPlayerSlotIndex is byte slotIndex && slotIndex < players.Length ? slotIndex : 0;
        DecodedInGamePlayer self = players[selfIndex];
        if (!self.IsEnabled || !self.IsInGame)
            return;

        if (
            !TryWorldToCanvas(
                self.PositionX,
                self.PositionY,
                self.RoomId,
                self.RoomName,
                canvasW,
                canvasH,
                out double selfCx,
                out double selfCy
            )
        )
            return;

        IBrush lineBrush = new SolidColorBrush(Color.FromArgb(200, 100, 220, 255));
        IBrush labelBackground = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0));

        for (int i = 0; i < players.Length; i++)
        {
            if (i == selfIndex)
                continue;

            DecodedInGamePlayer ally = players[i];
            if (!ally.IsEnabled || !ally.IsInGame)
                continue;

            if (
                !TryWorldToCanvas(
                    ally.PositionX,
                    ally.PositionY,
                    ally.RoomId,
                    ally.RoomName,
                    canvasW,
                    canvasH,
                    out double allyCx,
                    out double allyCy
                )
            )
                continue;

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

    private void UpdateOverlayLabels()
    {
        if (ZoomLabel is not null)
            ZoomLabel.Text = $"{_zoomLevel * 100:F0}%";

        if (CalibrationButton is not null)
            CalibrationButton.Content = _isCalibrationMode ? "Done" : "Calibrate";

        if (CalibrationLabel is null)
            return;

        if (!_isCalibrationMode || ViewModel is null)
        {
            CalibrationLabel.IsVisible = false;
            return;
        }

        string assetName = string.IsNullOrWhiteSpace(ViewModel.MapBackgroundRelativePath)
            ? "none"
            : System.IO.Path.GetFileName(ViewModel.MapBackgroundRelativePath);
        MapProjectionCalibration calibration = ViewModel.MapProjectionCalibration;

        CalibrationLabel.Text =
            $"Room {ViewModel.RoomId}: {ViewModel.RoomName}\n"
            + $"Asset: {assetName}\n"
            + $"ScaleX {calibration.ScaleX:F6}  ScaleY {calibration.ScaleY:F6}\n"
            + $"OffsetX {calibration.OffsetX:F4}  OffsetY {calibration.OffsetY:F4}\n"
            + "Arrows move, +/- scale, 0 reset, Esc close, Shift fine";
        CalibrationLabel.IsVisible = true;
    }

    private static bool IsEnemyBasicallyValid(DecodedEnemy enemy) =>
        !string.IsNullOrEmpty(enemy.Name) && enemy.RoomId != 0 && enemy is { SlotId: > 0, MaxHp: > 0 };

    protected void OnUnloaded()
    {
        DetachViewModel();
        _enemiesSubscription?.Dispose();
        _enemiesSubscription = null;
        _playersSubscription?.Dispose();
        _playersSubscription = null;
        _lastEnemies = null;
        _lastPlayers = null;

        base.OnUnloaded(null!);
    }

    private MapCanvasViewModel? ViewModel => DataContext as MapCanvasViewModel;
}
