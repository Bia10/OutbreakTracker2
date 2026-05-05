using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.UnitTests;

public sealed class ScenarioItemSlotViewModelTests
{
    private static readonly FieldInfo ItemField = typeof(ScenarioItemSlotViewModel).GetField(
        "_item",
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    [Test]
    public async Task EmptyHandgunDefault_UsesInventoryEmptyStyle()
    {
        ScenarioItemSlotViewModel viewModel = CreateViewModel(
            new DecodedItem
            {
                Id = 160,
                TypeId = 0,
                TypeName = "Handgun",
                Quantity = 0,
                PickedUp = 0,
                SlotIndex = 0,
            }
        );

        await Assert.That(viewModel.IsEmpty).IsTrue();
        await Assert.That(viewModel.DisplayName).IsEqualTo("Empty");
        await Assert.That(viewModel.DebugInfo).IsEqualTo("0x00 | 0");
    }

    [Test]
    public async Task ClearedSlotWithNonZeroSlotIndex_UsesInventoryEmptyStyle()
    {
        ScenarioItemSlotViewModel viewModel = CreateViewModel(
            new DecodedItem
            {
                Id = 160,
                TypeId = 0,
                TypeName = "Handgun",
                Quantity = 0,
                PickedUp = 0,
                SlotIndex = 7,
                Present = 0,
                RoomId = 5,
                RoomName = "Room 5",
            }
        );

        await Assert.That(viewModel.IsEmpty).IsTrue();
        await Assert.That(viewModel.DisplayName).IsEqualTo("Empty");
        await Assert.That(viewModel.DebugInfo).IsEqualTo("0x00 | 0");
    }

    [Test]
    public async Task LegitItem_IsNotInvalidatedByZeroPresentAndMix()
    {
        ScenarioItemSlotViewModel viewModel = CreateViewModel(
            new DecodedItem
            {
                Id = 17,
                TypeId = 10100,
                TypeName = "Staff Room Key",
                Quantity = 1,
                PickedUp = 0,
                SlotIndex = 0,
                Present = 0,
                Mix = 0,
            }
        );

        await Assert.That(viewModel.IsEmpty).IsFalse();
        await Assert.That(viewModel.DisplayName).IsEqualTo("Staff Room Key");
        await Assert.That(viewModel.DebugInfo).IsEqualTo("0x11 | 17");
    }

    [Test]
    public async Task InteractableZeroQuantityHandgun_DoesNotUseInventoryEmptyStyle()
    {
        ScenarioItemSlotViewModel viewModel = CreateViewModel(
            new DecodedItem
            {
                Id = 1,
                TypeId = 0,
                TypeName = "Handgun",
                Quantity = 0,
                PickedUp = 0,
                SlotIndex = 7,
                Present = 1,
            }
        );

        await Assert.That(viewModel.IsEmpty).IsFalse();
        await Assert.That(viewModel.DisplayName).IsEqualTo("Handgun");
        await Assert.That(viewModel.DebugInfo).IsEqualTo("0x01 | 1");
    }

    [Test]
    public async Task RealHandgunPickup_DoesNotUseInventoryEmptyStyle()
    {
        ScenarioItemSlotViewModel viewModel = CreateViewModel(
            new DecodedItem
            {
                Id = 1,
                TypeId = 0,
                TypeName = "Handgun",
                Quantity = 15,
                PickedUp = 0,
                SlotIndex = 1,
            }
        );

        await Assert.That(viewModel.IsEmpty).IsFalse();
        await Assert.That(viewModel.DisplayName).IsEqualTo("Handgun");
        await Assert.That(viewModel.DebugInfo).IsEqualTo("0x01 | 1");
    }

    [Test]
    public async Task UpdateItem_RaisesOrangeGlow_WhenSlotContentsChange()
    {
        ScenarioItemSlotViewModel viewModel = CreateLiveViewModel(
            new DecodedItem
            {
                Id = 1,
                TypeId = 10,
                TypeName = "Green Herb",
                Quantity = 1,
                Present = 1,
            }
        );

        Color? glowColor = null;
        viewModel.GlowTriggered += (_, args) => glowColor = args.Color;

        viewModel.UpdateItem(
            new DecodedItem
            {
                Id = 2,
                TypeId = 11,
                TypeName = "Handgun Ammo",
                Quantity = 15,
                Present = 1,
            },
            frameCounter: 100,
            GameFile.FileOne,
            positionIndex: 0
        );

        await Assert.That(glowColor.HasValue).IsTrue();
        await Assert.That(glowColor!.Value).IsEqualTo(Colors.Orange);
    }

    [Test]
    public async Task UpdateItem_RaisesRedGlow_WhenSlotEmpties()
    {
        ScenarioItemSlotViewModel viewModel = CreateLiveViewModel(
            new DecodedItem
            {
                Id = 1,
                TypeId = 10,
                TypeName = "Green Herb",
                Quantity = 1,
                Present = 1,
            }
        );

        Color? glowColor = null;
        viewModel.GlowTriggered += (_, args) => glowColor = args.Color;

        viewModel.UpdateItem(
            new DecodedItem
            {
                Id = 1,
                TypeId = 0,
                TypeName = string.Empty,
                Quantity = 0,
                PickedUp = 0,
                Present = 0,
            },
            frameCounter: 100,
            GameFile.FileOne,
            positionIndex: 0
        );

        await Assert.That(glowColor.HasValue).IsTrue();
        await Assert.That(glowColor!.Value).IsEqualTo(Colors.Red);
    }

    [Test]
    public async Task UpdateItem_DoesNotRaiseGlow_WhenSlotContentsStayTheSame()
    {
        DecodedItem item = new()
        {
            Id = 1,
            TypeId = 10,
            TypeName = "Green Herb",
            Quantity = 1,
            Present = 1,
        };

        ScenarioItemSlotViewModel viewModel = CreateLiveViewModel(item);

        bool glowTriggered = false;
        viewModel.GlowTriggered += (_, _) => glowTriggered = true;

        viewModel.UpdateItem(item, frameCounter: 100, GameFile.FileOne, positionIndex: 0);

        await Assert.That(glowTriggered).IsFalse();
    }

    [Test]
    public async Task ToggleMapProjectionCommand_TogglesProjectedStateAndHeader()
    {
        ScenarioItemSlotViewModel viewModel = CreateLiveViewModel(
            new DecodedItem
            {
                Id = 3,
                TypeId = 22,
                TypeName = "Handgun Ammo",
                Quantity = 15,
                Present = 1,
                RoomId = 4,
                RoomName = "Warehouse",
            }
        );

        await Assert.That(viewModel.IsProjectedOnMap).IsFalse();
        await Assert.That(viewModel.MapProjectionMenuHeader).IsEqualTo("Project on Map");

        viewModel.ToggleMapProjectionCommand.Execute(null);

        await Assert.That(viewModel.IsProjectedOnMap).IsTrue();
        await Assert.That(viewModel.MapProjectionMenuHeader).IsEqualTo("Remove from Map");
    }

    [Test]
    public async Task UpdateItem_ResetsMapProjection_WhenSlotIdentityChanges()
    {
        ScenarioItemSlotViewModel viewModel = CreateLiveViewModel(
            new DecodedItem
            {
                Id = 3,
                TypeId = 22,
                TypeName = "Handgun Ammo",
                Quantity = 15,
                Present = 1,
                RoomId = 4,
                RoomName = "Warehouse",
            }
        );

        viewModel.ToggleMapProjectionCommand.Execute(null);

        viewModel.UpdateItem(
            new DecodedItem
            {
                Id = 5,
                TypeId = 41,
                TypeName = "Green Herb",
                Quantity = 1,
                Present = 1,
                RoomId = 4,
                RoomName = "Warehouse",
            },
            frameCounter: 120,
            GameFile.FileOne,
            positionIndex: 0
        );

        await Assert.That(viewModel.IsProjectedOnMap).IsFalse();
        await Assert.That(viewModel.MapProjectionMenuHeader).IsEqualTo("Project on Map");
    }

    private static ScenarioItemSlotViewModel CreateViewModel(DecodedItem item)
    {
        ScenarioItemSlotViewModel viewModel = (ScenarioItemSlotViewModel)
            RuntimeHelpers.GetUninitializedObject(typeof(ScenarioItemSlotViewModel));

        ItemField.SetValue(viewModel, item);
        return viewModel;
    }

    private static ScenarioItemSlotViewModel CreateLiveViewModel(DecodedItem item) =>
        new(item, CreateItemImageViewModel(), GameFile.FileOne, positionIndex: 0);

    private static ItemImageViewModel CreateItemImageViewModel() =>
        new(NullLogger<ItemImageViewModel>.Instance, new StubImageViewModelFactory());

    private sealed class StubImageViewModelFactory : IImageViewModelFactory
    {
        public ImageViewModel Create() =>
            new(NullLogger<ImageViewModel>.Instance, new StubTextureAtlasService(), new ImmediateDispatcherService());
    }

    private sealed class StubTextureAtlasService : ITextureAtlasService
    {
        private static readonly ITextureAtlas EmptyAtlas = new StubTextureAtlas();

        public ITextureAtlas GetAtlas(string name) => EmptyAtlas;

        public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => new Dictionary<string, ITextureAtlas>();

        public Task LoadAtlasesAsync() => Task.CompletedTask;
    }

    private sealed class StubTextureAtlas : ITextureAtlas
    {
        public Bitmap? Texture => null;

        public bool TryGetSourceRectangle(string name, out Rect rect)
        {
            rect = default;
            return false;
        }

        public Rect GetSourceRectangle(string name) => default;
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action) => action();

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            TResult result = action();
            return Task.FromResult<TResult?>(result);
        }
    }
}
