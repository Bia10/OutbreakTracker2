using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Dock tool that hosts the embedded PCSX2 game window in the centre panel.
/// <para>
/// Dragging and floating are disabled because the game screen uses a Win32 NativeControlHost
/// (HWND child window) that always paints on top of Avalonia surfaces. Keeping it in a
/// fixed, non-overlapping layout cell avoids z-order conflicts.
/// </para>
/// </summary>
public sealed class GameScreenDockTool(EmbeddedGameViewModel embeddedGameViewModel) : Tool
{
    public EmbeddedGameViewModel EmbeddedGameViewModel { get; } = embeddedGameViewModel;
}
