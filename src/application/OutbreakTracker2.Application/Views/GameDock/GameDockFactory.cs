using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.GameDock.Dockables;
using Serilog;

namespace OutbreakTracker2.Application.Views.GameDock;

/// <summary>
/// Creates and initialises the docking layout for the Game Dock page.
/// <para>
/// Layout (horizontal proportional split):
/// <code>
/// ┌────────────┬────────────────────────────┬────────────────┐
/// │  Entities  │   Game Screen (Document)   │  Players (top) │
/// │  (left     │        [22%  ←  fill  →    │  ─────────────  │
/// │   22%,top) │           56%)             │ Scenario (bot) │
/// ├────────────┤                            │     (22%)      │
/// │  Items     │                            │                │
/// │  (left,bot)│                            │                │
/// └────────────┴────────────────────────────┴────────────────┘
/// </code>
/// </para>
/// <para>
/// The game screen document has <see cref="IDockable.CanFloat"/> and
/// <see cref="IDockable.CanDrag"/> disabled to prevent Win32 NativeControlHost
/// z-order conflicts when panels are dragged over the HWND area.
/// </para>
/// </summary>
public sealed class GameDockFactory(
    GameScreenTool gameScreenTool,
    EntitiesDockTool enemyListTool,
    MapDockTool mapDockTool,
    PlayersDockTool playersTool,
    ScenarioInfoDockTool scenarioInfoTool,
    ScenarioItemsDockTool scenarioItemsTool,
    ScenarioEnemiesDockTool scenarioEnemiesDockTool,
    ScenarioDoorsDockTool scenarioDoorsDockTool
) : Factory
{
    public override IToolDock CreateToolDock() => new GlobalToolDock();

    public override IRootDock CreateLayout()
    {
        gameScreenTool.Id = "GameScreen";
        gameScreenTool.Title = "Game Screen";
        gameScreenTool.CanClose = true;
        gameScreenTool.CanFloat = false; // float blocked to prevent Win32 HWND z-order conflicts
        gameScreenTool.CanDrag = true; // in-layout moves are safe; only float is hazardous

        enemyListTool.Id = "Entities";
        enemyListTool.Title = "Entities";
        enemyListTool.CanClose = true;
        enemyListTool.CanFloat = false;
        enemyListTool.CanDrag = true;
        enemyListTool.CanDrop = true;
        enemyListTool.CanPin = true;

        mapDockTool.Id = "Map";
        mapDockTool.Title = "Map";
        mapDockTool.CanClose = true;
        mapDockTool.CanFloat = false;
        mapDockTool.CanDrag = true;
        mapDockTool.CanDrop = true;
        mapDockTool.CanPin = true;

        playersTool.Id = "Players";
        playersTool.Title = "Players";
        playersTool.CanClose = true;
        playersTool.CanFloat = false;
        playersTool.CanDrag = true;
        playersTool.CanDrop = true;
        playersTool.CanPin = true;

        scenarioInfoTool.Id = "ScenarioInfo";
        scenarioInfoTool.Title = "Scenario";
        scenarioInfoTool.CanClose = true;
        scenarioInfoTool.CanFloat = false;
        scenarioInfoTool.CanDrag = true;
        scenarioInfoTool.CanDrop = true;
        scenarioInfoTool.CanPin = true;

        scenarioItemsTool.Id = "ScenarioItems";
        scenarioItemsTool.Title = "Items";
        scenarioItemsTool.CanClose = true;
        scenarioItemsTool.CanFloat = false;
        scenarioItemsTool.CanDrag = true;
        scenarioItemsTool.CanDrop = true;
        scenarioItemsTool.CanPin = true;

        scenarioEnemiesDockTool.Id = "ScenarioEnemies";
        scenarioEnemiesDockTool.Title = "Enemies";
        scenarioEnemiesDockTool.CanClose = true;
        scenarioEnemiesDockTool.CanFloat = false;
        scenarioEnemiesDockTool.CanDrag = true;
        scenarioEnemiesDockTool.CanDrop = true;
        scenarioEnemiesDockTool.CanPin = true;

        scenarioDoorsDockTool.Id = "ScenarioDoors";
        scenarioDoorsDockTool.Title = "Doors";
        scenarioDoorsDockTool.CanClose = true;
        scenarioDoorsDockTool.CanFloat = false;
        scenarioDoorsDockTool.CanDrag = true;
        scenarioDoorsDockTool.CanDrop = true;
        scenarioDoorsDockTool.CanPin = true;

        var leftDock = new ProportionalDock
        {
            Id = "LeftDock",
            Title = "Left",
            Proportion = 0.22,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>(
                CreateGameToolDock("EntitiesToolDock", 0.6, Alignment.Left, enemyListTool),
                new ProportionalDockSplitter(),
                CreateGameToolDock("ItemsToolDock", 0.4, Alignment.Left, scenarioItemsTool)
            ),
        };

        var centerDock = CreateToolDock();
        centerDock.Id = "CenterDock";
        centerDock.Title = "Game Screen";
        centerDock.Proportion = 0.56; // explicit so PSP can redistribute to left/right when center collapses
        centerDock.ActiveDockable = gameScreenTool;
        centerDock.VisibleDockables = CreateList<IDockable>(gameScreenTool);
        centerDock.GripMode = GripMode.Hidden;
        centerDock.IsCollapsable = false;
        centerDock.CanDrop = true;

        var rightDock = new ProportionalDock
        {
            Id = "RightDock",
            Title = "Right",
            Proportion = 0.22,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>(
                CreateGameToolDock("PlayersToolDock", 0.6, Alignment.Right, playersTool),
                new ProportionalDockSplitter(),
                CreateGameToolDock("ScenarioToolDock", 0.4, Alignment.Right, scenarioInfoTool)
            ),
        };

        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Title = "Main",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                leftDock,
                new ProportionalDockSplitter(),
                centerDock,
                new ProportionalDockSplitter(),
                rightDock
            ),
        };

        var rootDock = CreateRootDock();
        rootDock.Id = "Root";
        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        return rootDock;
    }

    private IToolDock CreateGameToolDock(
        string id,
        double proportion,
        Alignment alignment,
        IDockable activeDockable,
        params IDockable[] additionalDockables
    )
    {
        var toolDock = CreateToolDock();
        toolDock.Id = id;
        toolDock.Proportion = proportion;
        toolDock.ActiveDockable = activeDockable;
        toolDock.VisibleDockables = CreateList<IDockable>([activeDockable, .. additionalDockables]);
        toolDock.Alignment = alignment;
        toolDock.GripMode = GripMode.Visible;
        toolDock.IsCollapsable = false;
        toolDock.CanDrop = true;
        return toolDock;
    }

    public override void InitLayout(IDockable layout)
    {
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>(StringComparer.Ordinal)
        {
            [nameof(IDockWindow)] = static () => new DiagnosticHostWindow(),
        };

        base.InitLayout(layout);
    }

    public override IDockWindow? CreateWindowFrom(IDockable dockable)
    {
        var dockWindow = base.CreateWindowFrom(dockable);

        if (dockWindow is null)
        {
            return null;
        }

        dockWindow.Factory = this;

        if (dockWindow.Layout is { } floatingRoot)
        {
            floatingRoot.Factory = this;
            floatingRoot.Window = dockWindow;
            floatingRoot.IsCollapsable = true;
        }

        if (
            dockWindow.Layout is { VisibleDockables: { Count: > 0 } visibleDockables }
            && visibleDockables[0] is IToolDock floatingToolDock
        )
        {
            var sourceToolDock = dockable.OriginalOwner as IToolDock ?? dockable.Owner as IToolDock;

            floatingToolDock.Factory = this;
            floatingToolDock.ActiveDockable ??= dockable;
            floatingToolDock.FocusedDockable ??= dockable;
            floatingToolDock.DefaultDockable ??= dockable;
            floatingToolDock.IsCollapsable = true;
            floatingToolDock.CanDrag = sourceToolDock?.CanDrag ?? dockable.CanDrag;
            floatingToolDock.CanDrop = sourceToolDock?.CanDrop ?? dockable.CanDrop;
            floatingToolDock.CanFloat = sourceToolDock?.CanFloat ?? dockable.CanFloat;
            floatingToolDock.CanPin = sourceToolDock?.CanPin ?? dockable.CanPin;
        }

        if (dockWindow.Layout is IRootDock floatingLayout)
        {
            SyncFloatingRootSelection(floatingLayout, dockable);
        }

        return dockWindow;
    }

    private static void CaptureOriginalOwner(IDockable dockable, IDock? owner = null)
    {
        if (dockable.OriginalOwner is not null)
        {
            return;
        }

        if ((owner ?? dockable.Owner as IDock) is { } originalOwner)
        {
            dockable.OriginalOwner = originalOwner;
        }
    }

    /// <summary>
    /// Floating is disabled — see doc/SukiUI_Dock_Integration_Issues.md, Issue 12.
    /// </summary>
    public override void FloatDockable(IDockable dockable)
    {
        Log.Debug("[DockDiag] FloatDockable BLOCKED for {Dockable}", dockable.Title);
    }

    public override void PinDockable(IDockable dockable)
    {
        if (TryRestoreFloatingToolToOriginalOwner(dockable))
        {
            return;
        }

        if (TryRestoreDockedToolToOriginalOwner(dockable))
        {
            return;
        }

        base.PinDockable(dockable);
    }

    internal bool TryCompleteFailedFloatingFillDrop(IDockable dockable, IDock targetDock)
    {
        if (dockable is not ITool || dockable.Owner is not IDock sourceDock)
        {
            return false;
        }

        if (!IsInFloatingWindow(dockable) || sourceDock == targetDock || !targetDock.CanDrop)
        {
            return false;
        }

        if (FindRoot(targetDock, static _ => true) is null)
        {
            return false;
        }

        if (sourceDock is IToolDock floatingToolDock)
        {
            floatingToolDock.IsCollapsable = true;
        }

        var sourceRoot = FindRoot(sourceDock, static _ => true) as IRootDock;
        var sourceWindow = sourceRoot?.Window;
        var sourceHostWindow = sourceWindow?.Host as HostWindow;

        var targetInsertionIndex = GetDockInsertionIndex(targetDock);

        RemoveDockable(dockable, collapse: true);
        InsertDockable(targetDock, dockable, targetInsertionIndex);
        targetDock.ActiveDockable = dockable;
        targetDock.FocusedDockable = dockable;
        CollapseEmptyFloatingSourceDock(sourceDock, sourceRoot, sourceWindow, sourceHostWindow);
        UpdateDockingWindowStateRecursive(targetDock);
        UpdateDockingWindowStateRecursive(sourceDock);
        UpdateDockingWindowStateRecursive(dockable);

        dockable.OriginalOwner = null;

        Log.Debug(
            "[DockDiag] ManualFillDropCompleted sourceDock={SourceDock} targetDock={TargetDock} dockable={Dockable}",
            sourceDock.Id,
            targetDock.Id,
            dockable.Title
        );

        return true;
    }

    private bool TryRestoreFloatingToolToOriginalOwner(IDockable dockable)
    {
        if (
            dockable is not ITool
            || dockable.Owner is not IDock sourceDock
            || dockable.OriginalOwner is not IDock originalDock
        )
        {
            return false;
        }

        if (!IsInFloatingWindow(dockable) || sourceDock == originalDock)
        {
            return false;
        }

        if (FindRoot(originalDock, static _ => true) is null)
        {
            return false;
        }

        if (sourceDock is IToolDock floatingToolDock)
        {
            floatingToolDock.IsCollapsable = true;
        }

        var sourceRoot = FindRoot(sourceDock, static _ => true) as IRootDock;
        var sourceWindow = sourceRoot?.Window;
        var sourceHostWindow = sourceWindow?.Host as HostWindow;

        var targetInsertionIndex = GetDockInsertionIndex(originalDock);

        RemoveDockable(dockable, collapse: true);
        InsertDockable(originalDock, dockable, targetInsertionIndex);
        originalDock.ActiveDockable = dockable;
        originalDock.FocusedDockable = dockable;
        CollapseEmptyFloatingSourceDock(sourceDock, sourceRoot, sourceWindow, sourceHostWindow);
        UpdateDockingWindowStateRecursive(originalDock);
        UpdateDockingWindowStateRecursive(sourceDock);
        UpdateDockingWindowStateRecursive(dockable);

        dockable.OriginalOwner = null;

        return true;
    }

    private bool TryRestoreDockedToolToOriginalOwner(IDockable dockable)
    {
        if (
            dockable is not ITool
            || dockable.Owner is not IDock currentDock
            || dockable.OriginalOwner is not IDock originalDock
        )
        {
            return false;
        }

        if (currentDock == originalDock)
        {
            // Already home — just clear the stale flag.
            dockable.OriginalOwner = null;
            return false;
        }

        if (FindRoot(originalDock, static _ => true) is null)
        {
            // Original dock no longer in the tree; clear and fall through.
            dockable.OriginalOwner = null;
            return false;
        }

        var targetInsertionIndex = GetDockInsertionIndex(originalDock);

        RemoveDockable(dockable, collapse: true);
        InsertDockable(originalDock, dockable, targetInsertionIndex);
        originalDock.ActiveDockable = dockable;
        originalDock.FocusedDockable = dockable;
        UpdateDockingWindowStateRecursive(originalDock);
        UpdateDockingWindowStateRecursive(currentDock);
        UpdateDockingWindowStateRecursive(dockable);

        dockable.OriginalOwner = null;

        Log.Debug(
            "[DockDiag] RestoredDockedToolToOriginalOwner sourceDock={SourceDock} targetDock={TargetDock} dockable={Dockable}",
            currentDock.Id,
            originalDock.Id,
            dockable.Title
        );

        return true;
    }

    private static int GetDockInsertionIndex(IDock dock)
    {
        return dock.VisibleDockables?.Count ?? 0;
    }

    private void CollapseEmptyFloatingSourceDock(
        IDock sourceDock,
        IRootDock? sourceRoot,
        IDockWindow? sourceWindow,
        HostWindow? sourceHostWindow
    )
    {
        if (sourceRoot is null)
        {
            return;
        }

        if (!sourceDock.IsEmpty)
        {
            sourceDock.ActiveDockable = null;
            sourceDock.FocusedDockable = null;
            sourceDock.DefaultDockable = null;
            sourceDock.VisibleDockables?.Clear();
        }

        sourceDock.IsCollapsable = true;
        CollapseDock(sourceDock);

        sourceRoot.ActiveDockable = null;
        sourceRoot.FocusedDockable = null;
        sourceRoot.DefaultDockable = null;

        if (sourceRoot.VisibleDockables is { Count: > 0 } rootVisibleDockables)
        {
            rootVisibleDockables.Clear();
        }

        sourceRoot.IsCollapsable = true;
        CollapseDock(sourceRoot);
        UpdateDockingWindowStateRecursive(sourceRoot);

        Log.Debug(
            "[DockDiag] EmptyFloatingCleanupState sourceRoot={SourceRoot} sourceDock={SourceDock} sourceDockEmpty={SourceDockEmpty} sourceRootEmpty={SourceRootEmpty} sourceRootOpened={SourceRootOpened} sourceRootVisibleCount={SourceRootVisibleCount} sourceRootHost={SourceRootHost} sourceWindowCaptured={SourceWindowCaptured}",
            sourceRoot.Id,
            sourceDock.Id,
            sourceDock.IsEmpty,
            sourceRoot.IsEmpty,
            sourceRoot.OpenedDockablesCount,
            sourceRoot.VisibleDockables?.Count ?? 0,
            sourceHostWindow?.GetType().Name ?? "null",
            sourceWindow is not null
        );

        if (!ShouldCloseFloatingRootWindow(sourceRoot, sourceDock))
        {
            return;
        }

        Log.Debug(
            "[DockDiag] ForcedEmptyFloatingWindowClose sourceRoot={SourceRoot} sourceDock={SourceDock}",
            sourceRoot.Id,
            sourceDock.Id
        );

        if (sourceHostWindow is not null)
        {
            try
            {
                sourceHostWindow.Exit();
            }
            catch (Exception ex)
            {
                Log.Debug(
                    ex,
                    "[DockDiag] ForcedEmptyFloatingHostExitFailed sourceRoot={SourceRoot} sourceDock={SourceDock}",
                    sourceRoot.Id,
                    sourceDock.Id
                );
            }
        }
        else if (sourceWindow is not null)
        {
            try
            {
                RemoveWindow(sourceWindow);
            }
            catch (Exception ex)
            {
                Log.Debug(
                    ex,
                    "[DockDiag] ForcedEmptyFloatingWindowRemoveFailed sourceRoot={SourceRoot} sourceDock={SourceDock}",
                    sourceRoot.Id,
                    sourceDock.Id
                );
            }
        }

        if (sourceWindow is not null && sourceHostWindow?.IsTracked != true)
        {
            RemoveUntrackedFloatingWindowModel(sourceWindow);
        }

        sourceRoot.Window = null;
    }

    private void RemoveUntrackedFloatingWindowModel(IDockWindow sourceWindow)
    {
        var ownerRoot = sourceWindow.Owner as IRootDock;
        var removedFromOwner = ownerRoot?.Windows?.Remove(sourceWindow) ?? false;

        if (removedFromOwner)
        {
            OnWindowRemoved(sourceWindow);
        }

        sourceWindow.Host = null;
        sourceWindow.Layout = null;
        sourceWindow.Owner = null;

        Log.Debug(
            "[DockDiag] RemovedUntrackedFloatingWindowModel windowRemoved={WindowRemoved} ownerRoot={OwnerRoot} ownerWindowCount={OwnerWindowCount}",
            removedFromOwner,
            ownerRoot?.Id ?? "null",
            ownerRoot?.Windows?.Count ?? -1
        );
    }

    private static bool ShouldCloseFloatingRootWindow(IRootDock sourceRoot, IDock sourceDock)
    {
        if (sourceRoot.IsEmpty || sourceRoot.OpenedDockablesCount == 0)
        {
            return true;
        }

        if (sourceRoot.VisibleDockables is not { Count: > 0 } visibleDockables)
        {
            return true;
        }

        foreach (var dockable in visibleDockables)
        {
            if (!ReferenceEquals(dockable, sourceDock) && !dockable.IsEmpty)
            {
                return false;
            }
        }

        return true;
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

    private static readonly ILogger Log = Serilog.Log.ForContext<GameDockFactory>();

    /// <summary>
    /// Clears <see cref="IDockable.OriginalOwner"/> whenever Dock.Avalonia's
    /// own drag-drop path successfully moves a tool out of its floating window.
    /// Without this, the "Dock" context-menu item stays enabled and
    /// <see cref="PinDockable"/> would fall through to auto-hide.
    /// </summary>
    public override void OnDockableMoved(IDockable? dockable)
    {
        base.OnDockableMoved(dockable);

        if (dockable is ITool && dockable.OriginalOwner is not null && !IsInFloatingWindow(dockable))
        {
            dockable.OriginalOwner = null;
        }
    }

    private static void SyncFloatingLayoutSelection(IDockWindow? window)
    {
        if (window?.Layout is not IRootDock layout)
        {
            return;
        }

        var dockable = layout.FocusedDockable ?? (layout.ActiveDockable as IDock)?.ActiveDockable;
        dockable ??= layout.ActiveDockable;

        if (dockable is null)
        {
            return;
        }

        SyncFloatingRootSelection(layout, dockable);
    }

    private static void SyncFloatingRootSelection(IRootDock floatingRoot, IDockable dockable)
    {
        floatingRoot.FocusedDockable = dockable;

        var activeDockable = ResolveFloatingRootActiveDockable(floatingRoot, dockable);
        floatingRoot.ActiveDockable = activeDockable;
        floatingRoot.DefaultDockable = activeDockable;

        if (dockable.Owner is IDock owner)
        {
            owner.ActiveDockable = dockable;
            owner.FocusedDockable = dockable;
            owner.DefaultDockable ??= dockable;
        }
    }

    private static IDockable ResolveFloatingRootActiveDockable(IRootDock floatingRoot, IDockable dockable)
    {
        for (IDockable? current = dockable; current is not null; current = current.Owner)
        {
            if (ReferenceEquals(current.Owner, floatingRoot))
            {
                return current;
            }
        }

        return dockable;
    }

    public override bool OnWindowMoveDragBegin(IDockWindow? window)
    {
        SyncFloatingLayoutSelection(window);

        Log.Debug(
            "[DockDiag] WindowMoveDragBegin dockControls={Count} floatLayout={Layout} activeDockable={Title}",
            DockControls.Count,
            window?.Layout?.GetType().Name,
            window?.Layout?.ActiveDockable?.Title
        );
        return base.OnWindowMoveDragBegin(window);
    }

    public override void OnWindowMoveDrag(IDockWindow? window)
    {
        SyncFloatingLayoutSelection(window);

        Log.Debug(
            "[DockDiag] WindowMoveDrag dockControls={Count} activeDockable={Title}",
            DockControls.Count,
            window?.Layout?.ActiveDockable?.Title
        );
        base.OnWindowMoveDrag(window);
    }

    public override void OnWindowMoveDragEnd(IDockWindow? window)
    {
        SyncFloatingLayoutSelection(window);

        Log.Debug("[DockDiag] WindowMoveDragEnd layout={Layout}", window?.Layout?.ActiveDockable?.Title);
        base.OnWindowMoveDragEnd(window);
    }
}
