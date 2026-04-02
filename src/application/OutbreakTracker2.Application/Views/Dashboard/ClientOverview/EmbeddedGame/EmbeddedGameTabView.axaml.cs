using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;

public partial class EmbeddedGameTabView : UserControl
{
    public EmbeddedGameTabView()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Avalonia's TabControl toggles IsVisible on this control (the direct TabItem content)
        // when the user switches tabs.  EmbeddedGameView.IsVisible stays true (bound to
        // IsEmbedRequested), so the NativeControlHost is never destroyed — the embedded HWND
        // stays alive but goes black because PCSX2's D3D/GL swap chain never receives WM_SIZE.
        // We post the repaint at Render priority so Avalonia has already called
        // ShowWindow(SW_SHOW) on the native container before we fire WM_SIZE + InvalidateRect.
        if (change.Property.Equals(IsVisibleProperty) && change.GetNewValue<bool>())
        {
            Dispatcher.UIThread.Post(() => GameView.TriggerRepaint(), DispatcherPriority.Render);
        }
    }
}
