# Tracking Pipeline

The tracking flow is intentionally split into small stages:

- `DataManager` publishes raw entity snapshots from the current poll cycle.
- `EntityChangeSource<T>` turns snapshot arrays into warm, multicast diffs (`Added`, `Removed`, `Changed`).
- `EntityTracker<T>` evaluates alert rules over those diffs and emits `AlertNotification` values.
- `TrackerRegistry` composes the concrete trackers and exposes the aggregate alert stream used by the UI and notifications.

Keep the split in place when extending this area. Diffing and alert evaluation are separate responsibilities and should stay independently testable.