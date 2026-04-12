using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.UnitTests;

public sealed class ScenarioItemRoomGroupProjectionTests
{
    [Test]
    public async Task GetVisibleIndices_CollapsesFourContiguousStoryCopiesAndKeepsNormalDuplicates()
    {
        DecodedItem[] items =
        [
            CreateItem(id: 1, typeId: 300, typeName: "Green Herb", roomName: "B1 East", slotIndex: 1),
            CreateItem(id: 2, typeId: 10102, typeName: "Key with a Blue Tag", roomName: "B1 East", slotIndex: 0),
            CreateItem(id: 3, typeId: 10102, typeName: "Key with a Blue Tag", roomName: "B1 East", slotIndex: 0),
            CreateItem(id: 4, typeId: 10102, typeName: "Key with a Blue Tag", roomName: "B1 East", slotIndex: 7),
            CreateItem(id: 5, typeId: 10102, typeName: "Key with a Blue Tag", roomName: "B1 East", slotIndex: 0),
            CreateItem(id: 6, typeId: 300, typeName: "Green Herb", roomName: "B1 East", slotIndex: 2),
            CreateItem(id: 7, typeId: 300, typeName: "Green Herb", roomName: "B1 East", slotIndex: 3),
        ];

        IReadOnlyList<int> visibleIndices = ScenarioItemRoomGroupProjection.GetVisibleIndices(items);

        await Assert.That(visibleIndices.SequenceEqual([0, 3, 5, 6])).IsTrue();
    }

    [Test]
    public async Task GetVisibleIndices_DoesNotCollapseStoryCopiesAcrossDifferentRooms()
    {
        // 2 copies per room: each room collapses to its own representative (1 each), never merged
        DecodedItem[] items =
        [
            CreateItem(id: 1, typeId: 10100, typeName: "Staff Room Key", roomName: "1F West", slotIndex: 0),
            CreateItem(id: 2, typeId: 10100, typeName: "Staff Room Key", roomName: "1F West", slotIndex: 4),
            CreateItem(id: 3, typeId: 10100, typeName: "Staff Room Key", roomName: "2F East", slotIndex: 0),
            CreateItem(id: 4, typeId: 10100, typeName: "Staff Room Key", roomName: "2F East", slotIndex: 5),
        ];

        IReadOnlyList<int> visibleIndices = ScenarioItemRoomGroupProjection.GetVisibleIndices(items);

        // index 1 is the representative for "1F West" (slotIndex 4 > 0 wins over slotIndex 0)
        // index 3 is the representative for "2F East" (slotIndex 5 > 0 wins over slotIndex 0)
        await Assert.That(visibleIndices.SequenceEqual([1, 3])).IsTrue();
    }

    [Test]
    public async Task GetVisibleIndices_CollapsesNonContiguousCopiesInSameRoom()
    {
        // Underbelly-style interleaved layout: items stored player-by-player rather than key-by-key.
        // Key1 (11000) copies sit at raw indices 0, 2, 4, 6 — not consecutive.
        // Key2 (11001) copies sit at raw indices 1, 3, 5, 7 — not consecutive.
        DecodedItem[] items =
        [
            CreateItem(id: 1, typeId: 11000, typeName: "Employee Area Key", roomName: "Sewer 1F", slotIndex: 1),
            CreateItem(id: 2, typeId: 11001, typeName: "B2F Key", roomName: "Sewer 1F", slotIndex: 1),
            CreateItem(id: 3, typeId: 11000, typeName: "Employee Area Key", roomName: "Sewer 1F", slotIndex: 2),
            CreateItem(id: 4, typeId: 11001, typeName: "B2F Key", roomName: "Sewer 1F", slotIndex: 2),
            CreateItem(id: 5, typeId: 11000, typeName: "Employee Area Key", roomName: "Sewer 1F", slotIndex: 3),
            CreateItem(id: 6, typeId: 11001, typeName: "B2F Key", roomName: "Sewer 1F", slotIndex: 3),
            CreateItem(id: 7, typeId: 11000, typeName: "Employee Area Key", roomName: "Sewer 1F", slotIndex: 4),
            CreateItem(id: 8, typeId: 11001, typeName: "B2F Key", roomName: "Sewer 1F", slotIndex: 4),
        ];

        IReadOnlyList<int> visibleIndices = ScenarioItemRoomGroupProjection.GetVisibleIndices(items);

        // index 0 is representative for Employee Area Key (first slotIndex > 0)
        // index 1 is representative for B2F Key (first slotIndex > 0)
        await Assert.That(visibleIndices.SequenceEqual([0, 1])).IsTrue();
    }

    [Test]
    public async Task GetVisibleIndices_CollapsesPartialCopiesWhenSomePickedUp()
    {
        // After one player picks up their copy, NormalizeDisplayItem zeros out TypeId and moves it
        // to "Scenario Cleared". The remaining 3 copies in the original room must still collapse to 1.
        DecodedItem[] items =
        [
            new DecodedItem
            {
                Id = 1,
                SlotIndex = 0,
                TypeId = 0,
                TypeName = string.Empty,
                Quantity = 0,
                PickedUp = 0,
                Present = 0,
                Mix = 0,
                RoomName = "Scenario Cleared",
            },
            CreateItem(id: 2, typeId: 11000, typeName: "Employee Area Key", roomName: "Sewer 1F", slotIndex: 1),
            CreateItem(id: 3, typeId: 11000, typeName: "Employee Area Key", roomName: "Sewer 1F", slotIndex: 2),
            CreateItem(id: 4, typeId: 11000, typeName: "Employee Area Key", roomName: "Sewer 1F", slotIndex: 3),
        ];

        IReadOnlyList<int> visibleIndices = ScenarioItemRoomGroupProjection.GetVisibleIndices(items);

        // index 0: cleared slot (non-story, TypeId=0) shown once in "Scenario Cleared" room
        // index 1: representative of the 3 remaining copies in "Sewer 1F"
        await Assert.That(visibleIndices.SequenceEqual([0, 1])).IsTrue();
    }

    [Test]
    public async Task GetVisibleIndices_CollapsesCopiesWhenPresentValuesDiffer()
    {
        // Desperate Times Ace Key: each player copy has a different raw Present value
        // because Present stores a per-slot entity address, not a simple 0/1 flag.
        DecodedItem[] items =
        [
            new DecodedItem
            {
                Id = 1,
                SlotIndex = 1,
                TypeId = 11507,
                TypeName = "Ace Key",
                Quantity = 1,
                PickedUp = 0,
                Present = 3913792,
                Mix = 0,
                RoomName = "Waiting room",
            },
            new DecodedItem
            {
                Id = 2,
                SlotIndex = 2,
                TypeId = 11507,
                TypeName = "Ace Key",
                Quantity = 1,
                PickedUp = 0,
                Present = 3914176,
                Mix = 0,
                RoomName = "Waiting room",
            },
            new DecodedItem
            {
                Id = 3,
                SlotIndex = 3,
                TypeId = 11507,
                TypeName = "Ace Key",
                Quantity = 1,
                PickedUp = 0,
                Present = 3914560,
                Mix = 0,
                RoomName = "Waiting room",
            },
            new DecodedItem
            {
                Id = 4,
                SlotIndex = 4,
                TypeId = 11507,
                TypeName = "Ace Key",
                Quantity = 1,
                PickedUp = 0,
                Present = 3914944,
                Mix = 0,
                RoomName = "Waiting room",
            },
        ];

        IReadOnlyList<int> visibleIndices = ScenarioItemRoomGroupProjection.GetVisibleIndices(items);

        await Assert.That(visibleIndices.Count).IsEqualTo(1);
    }

    private static DecodedItem CreateItem(short id, short typeId, string typeName, string roomName, byte slotIndex)
    {
        return new DecodedItem
        {
            Id = id,
            SlotIndex = slotIndex,
            TypeId = typeId,
            TypeName = typeName,
            Quantity = 1,
            PickedUp = 0,
            Present = 1,
            Mix = 0,
            RoomName = roomName,
        };
    }
}
