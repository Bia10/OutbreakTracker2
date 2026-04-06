using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Serilog;

namespace OutbreakTracker2.Application.Views.GameDock;

/// <summary>
/// HostWindow subclass that applies the correct tool title and provides
/// drag-state diagnostic logging via Serilog at Debug level.
/// </summary>
public sealed class DiagnosticHostWindow : HostWindow
{
    private static readonly ILogger Log = Serilog.Log.ForContext<DiagnosticHostWindow>();
    private static readonly BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo? ChromeGripsField = typeof(HostWindow).GetField(
        "_chromeGrips",
        NonPublicInstance
    );
    private bool _isDragging;
    private string? _lastDragStateSnapshot;
    private IDock? _lastHoverTargetDock;
    private DockOperation _lastHoverIndicatorOperation;
    private bool _lastHoverGlobalFillValid;
    private bool _lastHoverDocumentFillFallback;
    private bool _lastHoverToolDockFillFallback;

    public DiagnosticHostWindow()
    {
        // ToolChromeControlsWholeWindow must be a local value so the ControlTheme
        // binding to OpenedDockablesCount cannot override it if count hasn't propagated.
        ToolChromeControlsWholeWindow = true;
        PositionChanged += OnPositionChanged;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        ApplyToolTitle();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // HostWindow.Present copies the owner window's title after OnDataContextChanged.
        // Re-apply the tool title so it takes effect after the copy.
        ApplyToolTitle();
    }

    private void ApplyToolTitle()
    {
        // ActiveDockable on a floating root is the wrapper IToolDock (Title = null),
        // so resolve the inner ITool title and set it as a local value instead.
        if (DataContext is IDock layout)
        {
            var inner =
                layout.FocusedDockable ?? (layout.ActiveDockable as IDock)?.ActiveDockable ?? layout.ActiveDockable;
            if (!string.IsNullOrWhiteSpace(inner?.Title))
            {
                Title = inner.Title;
            }
        }
    }

    private int GetChromeGripCount()
    {
        if (ChromeGripsField?.GetValue(this) is not IEnumerable chromeGrips)
        {
            return -1;
        }

        var count = 0;
        foreach (var _ in chromeGrips)
        {
            count++;
        }

        return count;
    }

    private bool IsPointerOverChromeGrip()
    {
        if (ChromeGripsField?.GetValue(this) is not IEnumerable chromeGrips)
        {
            return false;
        }

        foreach (var chromeGrip in chromeGrips)
        {
            if (chromeGrip is InputElement inputElement && inputElement.IsPointerOver)
            {
                return true;
            }
        }

        return false;
    }

    private string GetChromeGripSnapshot()
    {
        if (ChromeGripsField?.GetValue(this) is not IEnumerable chromeGrips)
        {
            return "unavailable";
        }

        var gripSnapshots = new List<string>();

        foreach (var chromeGrip in chromeGrips)
        {
            if (chromeGrip is InputElement inputElement)
            {
                var name = string.IsNullOrWhiteSpace(inputElement.Name) ? "-" : inputElement.Name;
                gripSnapshots.Add(
                    $"{inputElement.GetType().Name}(name={name}|bounds={inputElement.Bounds}|visible={inputElement.IsVisible}|hit={inputElement.IsHitTestVisible}|enabled={inputElement.IsEnabled}|over={inputElement.IsPointerOver})"
                );
            }
            else
            {
                gripSnapshots.Add(chromeGrip?.GetType().Name ?? "null");
            }
        }

        return gripSnapshots.Count == 0 ? "[]" : "[" + string.Join(", ", gripSnapshots) + "]";
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "Diagnostic logging reflects over Dock internal state only for local drag investigation."
    )]
    private static Control? GetDropControl(IHostWindowState hostWindowState)
    {
        var dockManagerStateType = hostWindowState.GetType().BaseType;
        var dropControlProperty = dockManagerStateType?.GetProperty("DropControl", NonPublicInstance);
        return dropControlProperty?.GetValue(hostWindowState) as Control;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "Diagnostic logging reflects over Dock internal adorner state only for local drag investigation."
    )]
    private static Control? GetLocalAdorner(IHostWindowState hostWindowState)
    {
        var dockManagerStateType = hostWindowState.GetType().BaseType;
        var localAdornerHelperProperty = dockManagerStateType?.GetProperty("LocalAdornerHelper", NonPublicInstance);
        return GetAdorner(localAdornerHelperProperty?.GetValue(hostWindowState));
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "Diagnostic logging reflects over Dock internal adorner state only for local drag investigation."
    )]
    private static Control? GetGlobalAdorner(IHostWindowState hostWindowState)
    {
        var dockManagerStateType = hostWindowState.GetType().BaseType;
        var globalAdornerHelperProperty = dockManagerStateType?.GetProperty("GlobalAdornerHelper", NonPublicInstance);
        return GetAdorner(globalAdornerHelperProperty?.GetValue(hostWindowState));
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "Diagnostic logging reflects over Dock internal adorner helpers only for local drag investigation."
    )]
    private static Control? GetAdorner(object? helper)
    {
        return helper?.GetType().GetField("Adorner", BindingFlags.Instance | BindingFlags.Public)?.GetValue(helper)
            as Control;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "Diagnostic logging reflects over Dock internal state only for local drag investigation."
    )]
    private static IDockManager? GetDockManager(IHostWindowState hostWindowState)
    {
        var dockManagerStateType = hostWindowState.GetType().BaseType;
        var dockManagerProperty = dockManagerStateType?.GetProperty("DockManager", NonPublicInstance);
        return dockManagerProperty?.GetValue(hostWindowState) as IDockManager;
    }

    private IDockable? GetFloatingDockable()
    {
        var layout = Window?.Layout;
        var dockable = layout?.FocusedDockable ?? (layout?.ActiveDockable as IDock)?.ActiveDockable;
        return dockable ?? layout?.ActiveDockable;
    }

    private bool CanCompleteGlobalFillDrop(IDock targetDock)
    {
        if (HostWindowState is null || GetDockManager(HostWindowState) is not { } dockManager)
        {
            return false;
        }

        if (GetFloatingDockable() is not { } sourceDockable)
        {
            return false;
        }

        return dockManager.ValidateDockable(sourceDockable, targetDock, DragAction.Move, DockOperation.Fill, false);
    }

    private void ClearHoverFallbackState()
    {
        _lastHoverTargetDock = null;
        _lastHoverIndicatorOperation = default;
        _lastHoverGlobalFillValid = false;
        _lastHoverDocumentFillFallback = false;
        _lastHoverToolDockFillFallback = false;
    }

    private static bool IsInFloatingWindow(IDockable dockable)
    {
        for (IDockable? current = dockable; current is not null; current = current.Owner)
        {
            if (current is IRootDock root)
            {
                return root.Window?.Owner is IRootDock;
            }
        }

        return false;
    }

    private void TryCompleteFailedFillDrop()
    {
        var canUseDocumentFillFallback = _lastHoverDocumentFillFallback && _lastHoverTargetDock is IDocumentDock;
        var canUseToolDockFillFallback = _lastHoverToolDockFillFallback && _lastHoverTargetDock is IToolDock;

        if (
            _lastHoverTargetDock is not { } targetDock
            || _lastHoverIndicatorOperation != DockOperation.Fill
            || (!_lastHoverGlobalFillValid && !canUseDocumentFillFallback && !canUseToolDockFillFallback)
        )
        {
            return;
        }

        if (GetFloatingDockable() is not { } dockable || !IsInFloatingWindow(dockable))
        {
            return;
        }

        if (Window?.Factory is not GameDockFactory factory)
        {
            return;
        }

        if (factory.TryCompleteFailedFloatingFillDrop(dockable, targetDock))
        {
            Log.Debug(
                "[DockDiag] ManualFillDropFallback targetDock={TargetDock} dockable={Dockable}",
                targetDock.Id,
                dockable.Title
            );
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "Diagnostic logging reflects over Dock adorner properties only for local drag investigation."
    )]
    private void LogDragHoverState()
    {
        if (HostWindowState is null)
        {
            return;
        }

        var dropControl = GetDropControl(HostWindowState);
        var localAdorner = GetLocalAdorner(HostWindowState);
        var globalAdorner = GetGlobalAdorner(HostWindowState);
        var localAdornerShowIndicatorsOnly = localAdorner
            ?.GetType()
            .GetProperty("ShowIndicatorsOnly", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(localAdorner);
        var globalAdornerShowIndicatorsOnly = globalAdorner
            ?.GetType()
            .GetProperty("ShowIndicatorsOnly", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(globalAdorner);
        var targetDock = dropControl?.DataContext as IDock;
        var globalFillValid = targetDock is null ? (bool?)null : CanCompleteGlobalFillDrop(targetDock);
        var indicatorOperation = dropControl is null
            ? default
            : Dock.Settings.DockProperties.GetIndicatorDockOperation(dropControl);
        var documentFillFallback = targetDock is IDocumentDock && indicatorOperation == DockOperation.Fill;
        var toolDockFillFallback =
            targetDock is IToolDock && !IsInFloatingWindow(targetDock) && indicatorOperation == DockOperation.Fill;

        if (targetDock is not null && (globalFillValid == true || documentFillFallback || toolDockFillFallback))
        {
            _lastHoverTargetDock = targetDock;
            _lastHoverIndicatorOperation = indicatorOperation;
            _lastHoverGlobalFillValid = globalFillValid == true;
            _lastHoverDocumentFillFallback = documentFillFallback;
            _lastHoverToolDockFillFallback = toolDockFillFallback;
        }
        else if (dropControl is null)
        {
            ClearHoverFallbackState();
        }

        var snapshot = string.Join(
            " | ",
            $"drop={dropControl?.GetType().Name ?? "null"}",
            $"dataContext={dropControl?.DataContext?.GetType().Name ?? "null"}",
            $"showIndicatorOnly={(dropControl is null ? "n/a" : Dock.Settings.DockProperties.GetShowDockIndicatorOnly(dropControl))}",
            $"indicatorOp={(dropControl is null ? "n/a" : Dock.Settings.DockProperties.GetIndicatorDockOperation(dropControl))}",
            $"isDockTarget={(dropControl is null ? "n/a" : dropControl.GetValue(Dock.Settings.DockProperties.IsDockTargetProperty))}",
            $"isDropEnabled={(dropControl is null ? "n/a" : dropControl.GetValue(Dock.Settings.DockProperties.IsDropEnabledProperty))}",
            $"adorner={localAdorner?.GetType().Name ?? "null"}",
            $"adornerIndicatorsOnly={localAdornerShowIndicatorsOnly ?? "n/a"}",
            $"globalAdorner={globalAdorner?.GetType().Name ?? "null"}",
            $"globalAdornerIndicatorsOnly={globalAdornerShowIndicatorsOnly ?? "n/a"}",
            $"globalFillValid={(globalFillValid.HasValue ? globalFillValid.Value : "n/a")}",
            $"documentFillFallback={documentFillFallback}",
            $"toolDockFillFallback={toolDockFillFallback}"
        );

        if (string.Equals(snapshot, _lastDragStateSnapshot, StringComparison.Ordinal))
        {
            return;
        }

        _lastDragStateSnapshot = snapshot;
        Log.Debug("[DockDiag] HoverState {Snapshot}", snapshot);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pointerPosition = e.GetPosition(this);
        var gripSnapshot = GetChromeGripSnapshot();

        Log.Debug(
            "[DockDiag] PointerPressed clientPos={ClientPos} windowPos={WindowPos} toolChrome={Chrome} dataCtx={Ctx} windowFactory={WindowFactory} layoutFactory={LayoutFactory} gripCount={GripCount} gripHot={GripHot} grips={GripSnapshot}",
            pointerPosition,
            Position,
            ToolChromeControlsWholeWindow,
            DataContext?.GetType().Name,
            Window?.Factory?.GetType().Name ?? "null",
            Window?.Layout?.Factory?.GetType().Name ?? "null",
            GetChromeGripCount(),
            IsPointerOverChromeGrip(),
            gripSnapshot
        );

        // Set _isDragging BEFORE calling base — base.OnPointerPressed calls
        // BeginMoveDrag which BLOCKS on Windows (Win32 modal loop). If we set
        // the flag after base returns, we miss every PositionChanged event
        // that fires during the drag.
        _isDragging = true;
        base.OnPointerPressed(e);

        // base.OnPointerPressed only returns once the drag modal loop exits.
        Log.Debug("[DockDiag] BeginMoveDrag returned (drag modal loop exited)");
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var pointerPosition = e.GetPosition(this);
        LogDragHoverState();
        Log.Debug("[DockDiag] PointerReleased clientPos={ClientPos} windowPos={WindowPos}", pointerPosition, Position);
        base.OnPointerReleased(e);
        TryCompleteFailedFillDrop();
        _isDragging = false;
        _lastDragStateSnapshot = null;
        ClearHoverFallbackState();
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (_isDragging)
        {
            Log.Debug("[DockDiag] PositionChanged during drag pos={Pos}", Position);
            LogDragHoverState();
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        Log.Debug(
            "[DockDiag] HostWindowClosing cancel={Cancel} tracked={Tracked} hasWindow={HasWindow} layout={Layout} activeDockable={ActiveDockable} visibleCount={VisibleCount}",
            e.Cancel,
            IsTracked,
            Window is not null,
            Window?.Layout?.GetType().Name ?? "null",
            Window?.Layout?.ActiveDockable?.Title ?? "null",
            (Window?.Layout as IDock)?.VisibleDockables?.Count ?? -1
        );

        base.OnClosing(e);

        Log.Debug(
            "[DockDiag] HostWindowClosingAfter cancel={Cancel} tracked={Tracked} hasWindow={HasWindow}",
            e.Cancel,
            IsTracked,
            Window is not null
        );
    }

    protected override void OnClosed(EventArgs e)
    {
        Log.Debug(
            "[DockDiag] HostWindowClosed tracked={Tracked} hasWindow={HasWindow} layout={Layout} activeDockable={ActiveDockable} gripCount={GripCount}",
            IsTracked,
            Window is not null,
            Window?.Layout?.GetType().Name ?? "null",
            Window?.Layout?.ActiveDockable?.Title ?? "null",
            GetChromeGripCount()
        );

        PositionChanged -= OnPositionChanged;
        base.OnClosed(e);
    }
}
