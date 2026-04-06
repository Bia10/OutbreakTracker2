using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;

namespace OutbreakTracker2.Application.Views.GameDock;

/// <summary>
/// ToolDock that can participate in Dock.Avalonia's global docking path.
/// Floating window drops over tool docks otherwise resolve to a local Fill
/// operation, which HostWindowState rejects and re-floats on release.
/// </summary>
public sealed class GlobalToolDock : ToolDock, IGlobalTarget;
