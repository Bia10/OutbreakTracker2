using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

public static class ScenarioItemRoomGroupProjection
{
    private const short StoryItemTypeIdThreshold = 10_000;

    public static IReadOnlyList<int> GetVisibleIndices(IReadOnlyList<DecodedItem> items) =>
        GetVisibleIndicesCore(items, static item => item);

    public static IReadOnlyList<int> GetVisibleIndices(IReadOnlyList<ScenarioItemSlotViewModel> items) =>
        GetVisibleIndicesCore(items, static item => item.Item);

    private static IReadOnlyList<int> GetVisibleIndicesCore<TItem>(
        IReadOnlyList<TItem> items,
        Func<TItem, DecodedItem> getItem
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(getItem);

        Dictionary<string, List<int>> roomIndices = new(StringComparer.Ordinal);

        for (int index = 0; index < items.Count; index++)
        {
            string roomName = NormalizeRoomName(getItem(items[index]).RoomName);
            if (!roomIndices.TryGetValue(roomName, out List<int>? indices))
            {
                indices = [];
                roomIndices.Add(roomName, indices);
            }

            indices.Add(index);
        }

        List<int> visibleIndices = new(items.Count);
        foreach (List<int> indices in roomIndices.Values)
            AddVisibleIndicesForRoom(items, indices, visibleIndices, getItem);

        visibleIndices.Sort();
        return visibleIndices;
    }

    private static void AddVisibleIndicesForRoom<TItem>(
        IReadOnlyList<TItem> items,
        IReadOnlyList<int> roomIndices,
        List<int> visibleIndices,
        Func<TItem, DecodedItem> getItem
    )
    {
        HashSet<int> collapsed = new(roomIndices.Count);

        for (int index = 0; index < roomIndices.Count; index++)
        {
            int rawIndex = roomIndices[index];
            if (collapsed.Contains(rawIndex))
                continue;

            DecodedItem candidate = getItem(items[rawIndex]);

            if (!IsStoryItem(candidate.TypeId))
            {
                visibleIndices.Add(rawIndex);
                continue;
            }

            // Collect all matching copies of this story item anywhere in the room,
            // regardless of their position in the raw array. This handles:
            //   - interleaved-by-player memory layouts (non-contiguous raw indices)
            //   - partial pickups where fewer than 4 copies remain in the room
            List<int> copies = [rawIndex];
            for (int j = index + 1; j < roomIndices.Count; j++)
            {
                int otherRawIndex = roomIndices[j];
                if (
                    !collapsed.Contains(otherRawIndex)
                    && AreMirroredStoryCopies(candidate, getItem(items[otherRawIndex]))
                )
                    copies.Add(otherRawIndex);
            }

            visibleIndices.Add(SelectRepresentativeIndex(items, copies, getItem));
            foreach (int ri in copies)
                collapsed.Add(ri);
        }
    }

    private static bool AreMirroredStoryCopies(in DecodedItem candidate, in DecodedItem current)
    {
        // Present is a raw per-slot value from the pickup-space struct (not a simple 0/1 flag);
        // it differs between player copies. Exclude it from identity — only TypeId, room,
        // quantity, pickup state, and mix determine whether two slots are copies of the same key.
        return current.TypeId == candidate.TypeId
            && current.Quantity == candidate.Quantity
            && current.PickedUp == candidate.PickedUp
            && current.Mix == candidate.Mix
            && string.Equals(
                NormalizeRoomName(current.RoomName),
                NormalizeRoomName(candidate.RoomName),
                StringComparison.Ordinal
            );
    }

    private static int SelectRepresentativeIndex<TItem>(
        IReadOnlyList<TItem> items,
        List<int> copies,
        Func<TItem, DecodedItem> getItem
    )
    {
        foreach (int rawIndex in copies)
        {
            if (getItem(items[rawIndex]).SlotIndex > 0)
                return rawIndex;
        }

        return copies[0];
    }

    private static bool IsStoryItem(short typeId) => typeId >= StoryItemTypeIdThreshold;

    private static string NormalizeRoomName(string roomName) => string.IsNullOrEmpty(roomName) ? "Unknown" : roomName;
}
