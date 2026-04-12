using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

public static class ScenarioItemRoomGroupProjection
{
    private const int MirroredStoryItemCopyCount = 4;
    private const short StoryItemTypeIdThreshold = 10_000;

    public static IReadOnlyList<int> GetVisibleIndices(IReadOnlyList<DecodedItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Dictionary<string, List<int>> roomIndices = new(StringComparer.Ordinal);

        for (int index = 0; index < items.Count; index++)
        {
            string roomName = NormalizeRoomName(items[index].RoomName);
            if (!roomIndices.TryGetValue(roomName, out List<int>? indices))
            {
                indices = [];
                roomIndices.Add(roomName, indices);
            }

            indices.Add(index);
        }

        List<int> visibleIndices = new(items.Count);
        foreach (List<int> indices in roomIndices.Values)
            AddVisibleIndicesForRoom(items, indices, visibleIndices);

        visibleIndices.Sort();
        return visibleIndices;
    }

    private static void AddVisibleIndicesForRoom(
        IReadOnlyList<DecodedItem> items,
        IReadOnlyList<int> roomIndices,
        List<int> visibleIndices
    )
    {
        for (int index = 0; index < roomIndices.Count; index++)
        {
            if (TryGetMirroredStoryRepresentativeIndex(items, roomIndices, index, out int representativeIndex))
            {
                visibleIndices.Add(representativeIndex);
                index += MirroredStoryItemCopyCount - 1;
                continue;
            }

            visibleIndices.Add(roomIndices[index]);
        }
    }

    private static bool TryGetMirroredStoryRepresentativeIndex(
        IReadOnlyList<DecodedItem> items,
        IReadOnlyList<int> roomIndices,
        int startIndex,
        out int representativeIndex
    )
    {
        representativeIndex = -1;

        if (startIndex + MirroredStoryItemCopyCount > roomIndices.Count)
            return false;

        int firstRawIndex = roomIndices[startIndex];
        DecodedItem candidate = items[firstRawIndex];
        if (!IsStoryItem(candidate.TypeId))
            return false;

        for (int offset = 1; offset < MirroredStoryItemCopyCount; offset++)
        {
            int currentRawIndex = roomIndices[startIndex + offset];
            if (currentRawIndex != firstRawIndex + offset)
                return false;

            if (!AreMirroredStoryCopies(candidate, items[currentRawIndex]))
                return false;
        }

        representativeIndex = SelectRepresentativeIndex(items, roomIndices, startIndex);
        return true;
    }

    private static bool AreMirroredStoryCopies(in DecodedItem candidate, in DecodedItem current)
    {
        return current.TypeId == candidate.TypeId
            && current.Quantity == candidate.Quantity
            && current.PickedUp == candidate.PickedUp
            && current.Present == candidate.Present
            && current.Mix == candidate.Mix
            && string.Equals(
                NormalizeRoomName(current.RoomName),
                NormalizeRoomName(candidate.RoomName),
                StringComparison.Ordinal
            );
    }

    private static int SelectRepresentativeIndex(
        IReadOnlyList<DecodedItem> items,
        IReadOnlyList<int> roomIndices,
        int startIndex
    )
    {
        for (int offset = 0; offset < MirroredStoryItemCopyCount; offset++)
        {
            int rawIndex = roomIndices[startIndex + offset];
            if (items[rawIndex].SlotIndex > 0)
                return rawIndex;
        }

        return roomIndices[startIndex];
    }

    private static bool IsStoryItem(short typeId) => typeId >= StoryItemTypeIdThreshold;

    private static string NormalizeRoomName(string roomName) => string.IsNullOrEmpty(roomName) ? "Unknown" : roomName;
}
