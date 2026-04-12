using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.Factory;

public sealed class InGamePlayerSubViewModelFactory(IItemSlotViewModelFactory itemSlotViewModelFactory)
    : IInGamePlayerSubViewModelFactory
{
    private readonly IItemSlotViewModelFactory _itemSlotViewModelFactory = itemSlotViewModelFactory;

    public OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerGaugesViewModel CreateGauges() =>
        new();

    public OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerStatusEffectsViewModel CreateStatusEffects() =>
        new();

    public OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerConditionsViewModel CreateConditions() =>
        new();

    public OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerAttributesViewModel CreateAttributes() =>
        new();

    public OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.PlayerPositionViewModel CreatePosition() =>
        new();

    public InventoryViewModel CreateInventory(DecodedInGamePlayer player) => new(player, _itemSlotViewModelFactory);
}
