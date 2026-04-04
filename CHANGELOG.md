# Changelog

Full release notes with attached binaries are on the [GitHub Releases page](https://github.com/Bia10/OutbreakTracker2/releases).

This project uses [MinVer](https://github.com/adamralph/minver) for git-tag-based versioning (`v`-prefixed tags).

To create a release: go to **Actions → Release → Run workflow** and enter a version number (e.g. `0.2.0`).

---

## [Unreleased]

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

