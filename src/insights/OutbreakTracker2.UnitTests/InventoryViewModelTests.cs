using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.UnitTests;

public sealed class InventoryViewModelTests
{
    [Test]
    public async Task UpdateFromPlayerData_ReusesExistingSlotViewModels()
    {
        InventoryViewModel viewModel = new(
            new DecodedInGamePlayer { Name = "Kevin", Status = "Fine" },
            new StubItemSlotViewModelFactory()
        );

        ItemSlotViewModel equippedSlot = viewModel.EquippedItems[0];
        ItemSlotViewModel mainSlot = viewModel.MainSlots[0];
        ItemSlotViewModel deadSlot = viewModel.DeadSlots[0];

        DecodedItem[] scenarioItems =
        [
            new()
            {
                Id = 1,
                TypeName = "Handgun",
                Quantity = 15,
                SlotIndex = 1,
            },
            new()
            {
                Id = 2,
                TypeName = "First Aid Spray",
                Quantity = 1,
                SlotIndex = 2,
            },
        ];

        viewModel.UpdateFromPlayerData(
            "Fine",
            equippedItem: 1,
            mainInventory: new InventorySnapshot(1, 2, 0, 0),
            specialItem: 0,
            specialInventory: InventorySnapshot.Empty,
            deadInventory: InventorySnapshot.Empty,
            specialDeadInventory: InventorySnapshot.Empty,
            GameFile.FileOne,
            scenarioItems
        );

        viewModel.UpdateFromPlayerData(
            "Fine",
            equippedItem: 2,
            mainInventory: new InventorySnapshot(2, 1, 0, 0),
            specialItem: 0,
            specialInventory: InventorySnapshot.Empty,
            deadInventory: InventorySnapshot.Empty,
            specialDeadInventory: InventorySnapshot.Empty,
            GameFile.FileOne,
            scenarioItems
        );

        await Assert.That(ReferenceEquals(equippedSlot, viewModel.EquippedItems[0])).IsTrue();
        await Assert.That(ReferenceEquals(mainSlot, viewModel.MainSlots[0])).IsTrue();
        await Assert.That(ReferenceEquals(deadSlot, viewModel.DeadSlots[0])).IsTrue();
    }

    private sealed class StubItemSlotViewModelFactory : IItemSlotViewModelFactory
    {
        public ItemSlotViewModel Create(int slotNumber) =>
            new(NullLogger<ItemSlotViewModel>.Instance, new StubItemImageViewModelFactory())
            {
                SlotNumber = slotNumber,
            };
    }

    private sealed class StubItemImageViewModelFactory : IItemImageViewModelFactory
    {
        public ItemImageViewModel Create() =>
            new(NullLogger<ItemImageViewModel>.Instance, new StubImageViewModelFactory());
    }

    private sealed class StubImageViewModelFactory : IImageViewModelFactory
    {
        public ImageViewModel Create() =>
            new(NullLogger<ImageViewModel>.Instance, new MissingAtlasService(), new ImmediateDispatcherService());
    }

    private sealed class MissingAtlasService : ITextureAtlasService
    {
        public ITextureAtlas GetAtlas(string name) => null!;

        public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => new Dictionary<string, ITextureAtlas>();

        public Task LoadAtlasesAsync() => Task.CompletedTask;
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action) => action();

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TResult?>(action());
        }
    }
}
