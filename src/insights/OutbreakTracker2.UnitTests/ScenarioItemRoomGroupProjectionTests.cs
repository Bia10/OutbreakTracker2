using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;
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
        DecodedItem[] items =
        [
            CreateItem(id: 1, typeId: 10100, typeName: "Staff Room Key", roomName: "1F West", slotIndex: 0),
            CreateItem(id: 2, typeId: 10100, typeName: "Staff Room Key", roomName: "1F West", slotIndex: 4),
            CreateItem(id: 3, typeId: 10100, typeName: "Staff Room Key", roomName: "2F East", slotIndex: 0),
            CreateItem(id: 4, typeId: 10100, typeName: "Staff Room Key", roomName: "2F East", slotIndex: 5),
        ];

        IReadOnlyList<int> visibleIndices = ScenarioItemRoomGroupProjection.GetVisibleIndices(items);

        await Assert.That(visibleIndices.SequenceEqual([0, 1, 2, 3])).IsTrue();
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
