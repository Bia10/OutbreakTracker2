# Changelog

Full release notes with attached binaries are on the [GitHub Releases page](https://github.com/Bia10/OutbreakTracker2/releases).

This project uses [MinVer](https://github.com/adamralph/minver) for git-tag-based versioning (`v`-prefixed tags).

To create a release: go to **Actions → Release → Run workflow** and enter a version number (e.g. `0.2.0`).

---

## [Unreleased]

### Added
- Tests: `ClientAlreadyRunningViewModelTests` and `InGameEnemyViewModelTests` new unit test files

### Changed
- Reports: `RunReportEnemyDiffProcessor` — inject `ILogger`; add `MaxReportableEnemyHp` / `MaxReportableEnemyDamage` validation constants; guard spawns via `TryValidateSpawn` and kills via `IsReportableMaxHp`; log faulty events instead of silently emitting them
- Reports: `HtmlRunReportWriter` — interactive event density timeline with focus/reset bar, accessibility roles, and hover caption; histogram scale labels added; default group-by switched to "Event type"
- Reports: `RunReportService` — forward logger to `RunReportEnemyDiffProcessor`
- Views: `ClientAlreadyRunningViewModel` — convert from primary constructor to explicit constructor; expose `RunningProcessesView` as `NotifyCollectionChangedSynchronizedViewList` for safe cross-thread binding
- Views: `ClientAlreadyRunningView` — bind `ItemsControl` to `RunningProcessesView`

### Fixed
- Enemy: `EnemyStatusUtility.GetHealthStatusForFileTwo` — replace `nameId == 0` guard with `IsEmptyFileTwoSlot(slotId, maxHp)` to correctly detect empty file-two slots
- Enemy: `InGameEnemyViewModel.GetEnemyColorForFileTwo` — normalize empty-slot fallback to `InvalidSlotColor` constant; remove dead commented-out code

---

## [v1.0.0] — 2026-04-13

### Added
- Settings: `ShowGameplayUiDuringTransitions` display setting to control UI visibility during non-stable scenario states
- Reports: HTML run report with full event density timeline, grouped event log, and composite writer support
- Reports: raw inventory slot values surfaced in HTML output; `InventorySlotValueResolver` introduced for shared slot resolution
- Reports: door events now include `SlotId`; enemy kill/despawn detection improved; room names resolved in event timeline and monster log
- Reports: `RunEvent.Accumulate` internal method for building event hierarchies
- Tracking: `ICurrentScenarioState` interface; alert rules narrowed to depend on `ICurrentScenarioState` instead of full `IDataSnapshot`; in-game reset gated on confirmed post-game lobby
- Tracking: `CollectionDiffAccumulator` extracted; `IReadOnlyEntityTracker` exposed; `TrackerRegistry` tightened
- Data: `GameStateStore` and `IDataObservableSource` / `IDataSnapshot` interfaces introduced
- Data: `DataManagerOptions` for configurable polling intervals
- Inventory: inventory snapshot model and domain utilities
- Library: `ScenarioStatus` extended values; `DecodedInGamePlayer` new fields; readers and utilities updated
- Views: in-game UI visibility gated via `ShouldShowGameplayUi` and improved overview view models
- Launcher: `GameClientConnectionService` added; `ProcessLauncher` cancellation propagation improved
- Embedding: Windows embedded-window refactored into focused session components
- Atlas: texture atlas service, sprite name resolver, and image view models refined

### Changed
- Reports: `RunReportService` decomposed into dedicated diff processors (`RunReportEnemyDiffProcessor`, `RunReportDoorDiffProcessor`, etc.)
- Tracking: `IAlertRule<T>` split into typed `IAddedAlertRule`, `IUpdatedAlertRule`, `IRemovedAlertRule`
- Scenario: `ScenarioEntitiesViewModel` split into `ScenarioItemsViewModel` and `ScenarioEnemiesViewModel`
- Outbreak: `RoomName` dropped from `DecodedEnemy`; player slot ULIDs migrated to `ConcurrentDictionary`
- Enemy: `EnemyStatusUtility` extracted from inline logic; `EnemyListReconciler` removed
- Views: `GlowEventArgs` consolidated into `Views/Common`
- Services: log-storage uses bounded buffer; process launcher lifetime cleanup

### Fixed
- Keyboard focus: restore focus to PCSX2 when OT2 regains foreground after alt-tab
- Startup: deferred dock rebuild and pre-warmed singletons to eliminate startup blocking
- Scenario items: collapse non-contiguous story item copies in projection
- Scenario state: prevent stale state when switching dashboard views
- Performance: incremental room-group patching to avoid full collection Reset events

---

## [v0.3.0] — 2026-04-06

### Added
- Dock: integrate SukiUI.Dock and vendor Dock control themes for full SukiUI visual consistency
- Dock: add scenario entity dock tools — Items, Enemies, Doors — as closable, pinnable tool panes
- Dock: add `ScenarioEntityCommands` wiring ShowItems / ShowEnemies / ShowDoors relay commands to dock float
- Dock: add `CollapseInPspWhenHiddenBehavior` for PSP layout mode
- Dock: add `AvaloniaFileSink` writing Avalonia-internal diagnostics (binding errors, theme resolution) to `logs/avaloniaLog.txt`
- Scenario: explicit DataGrid columns for Items, Enemies and Doors entity views (Slot / Type / Qty / Room / Held By / Mix / Present; Slot / Name / HP / MaxHP / Room / Boss / Status; HP / Flag / Status)
- Scenario: `ResolveItemDisplayFields` — populate `RoomName` via scenario enum extension and `PickedUpByName` from live player roster
- Model: add `RoomName` and `PickedUpByName` computed display fields to `DecodedItem`
- Inventory: add `ClearImageAsync` to `ImageViewModel` and `ItemImageViewModel` for explicit slot clearing

### Changed
- Dock: replace `DocumentDock` + `GameScreenDocument` center pane with `GlobalToolDock` + `GameScreenTool` so all panes share the same dock type and theme path
- Dock: move DataTemplates from `GameDockView` to `Application.DataTemplates` in `App.axaml` so they resolve in floating host windows
- Dock: replace `DockFluentTheme DensityStyle=Normal` with plain `DockFluentTheme`; load SukiUI.Dock resource includes
- Dock: `GameDockFactory` — add `CreateWindowFrom` / `FloatDockable` (blocked) / `PinDockable` / `OnDockableMoved` overrides plus floating-root sync and diagnostic window-drag hooks
- Dock: `CanFloat=false` on `GameScreenTool` to prevent Win32 HWND z-order conflicts; `CanDrag/Drop/Pin=true` on all other tools
- Scenario: replace SukiWindow popup dialogs with dock-integrated tool panes bound via `ScenarioEntityCommands`
- UI: tune log levels — promote `OutbreakTracker2` namespace to `Debug`; pin `OutbreakTracker2.Outbreak` and `OutbreakTracker2.Application.Services` back to `Information`

### Fixed
- Inventory: empty item slot did not clear previous sprite — inverted condition in `ItemSlotViewModel.Update` to call `ClearImageAsync` on empty slot
- UI: reduce default `ItemSlotWidth` 78→58, `ItemSlotHeight` 66→50 to fit docked player panel proportions

---

## [v0.2.0] — 2026-04-01

### Added
- Visualize inventory slot events with border glow and fade animation (#26)
- UI: player panel — condition/status on one row, responsive inventory columns per layout mode
- UI: scenario panel — compact entity buttons grid, open entities in SukiWindow
- UI: mob list — display label and value inline on same row
- UI: increase inventory slot size by 50% universally
- Map: zoom/pan controls, correct label placement, Euclidean distance overlay
- Map: make map a dockable component
- Universal no-in-game watermark
- Add PositionX/Y fields and placeholder offsets to enemy model

### Fixed
- Mob list and player list not auto-updating on scenario load
- Mob list and scenario reactiveness: reset in-game data when transitioning from scenario to lobby/non-game states (#24)
- Mob type detection: derive BossType from EnemyType classification (#20)
- Mob list: skull/shield icons, progress bar, 5-second death timer (#22)
- Map: Ctrl+hover now draws self→ally distances instead of cursor→all players (#18)
- Becoming zombie no longer fires 'Player Died' event (#16)
- NotInGameWatermark DataContext hijacking inherited DataContext
- NullReferenceException in DrawPlayers from uninitialized player slots
- Reduce log spam and remove invalid enemy map rendering

---

## [v0.1.0] — 2026-03-31

Initial public alpha release.

### Added
- Cross-platform support: Windows (.NET 10, fully supported) and Linux (.NET 10, partial)
- PCSX2 2.0.0+ process detection and attachment
- Player tracking: health, virus gauge, inventory, status conditions
- Mob list: enemy type, health, position tracking
- Scenario/lobby state detection
- Interactive map view with player position overlay
- Embedded/docked game screen mode (Windows only, experimental)
- Serilog structured logging with configurable sinks
- CI: separate CI and Release workflows with SHA-pinned actions

