using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

public partial class ScenarioItemSlotView : UserControl
{
    public ScenarioItemSlotView()
    {
        InitializeComponent();
    }

    private void OnDetailsMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (ItemCard is not null)
            FlyoutBase.ShowAttachedFlyout(ItemCard);
    }

    private void OnDetailsFlyoutClose(object? sender, RoutedEventArgs e)
    {
        if (ItemCard?.GetValue(FlyoutBase.AttachedFlyoutProperty) is FlyoutBase flyout)
            flyout.Hide();
    }
}
