using System.Reflection;
using System.Runtime.CompilerServices;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;
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

    private static ScenarioItemSlotViewModel CreateViewModel(DecodedItem item)
    {
        ScenarioItemSlotViewModel viewModel = (ScenarioItemSlotViewModel)
            RuntimeHelpers.GetUninitializedObject(typeof(ScenarioItemSlotViewModel));

        ItemField.SetValue(viewModel, item);
        return viewModel;
    }
}
