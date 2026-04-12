using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.Factory;

public interface IInGamePlayerSubViewModelFactory
{
    OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerGaugesViewModel CreateGauges();

    OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerStatusEffectsViewModel CreateStatusEffects();

    OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerConditionsViewModel CreateConditions();

    OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerAttributesViewModel CreateAttributes();

    OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerPositionViewModel CreatePosition();

    InventoryViewModel CreateInventory(DecodedInGamePlayer player);
}
