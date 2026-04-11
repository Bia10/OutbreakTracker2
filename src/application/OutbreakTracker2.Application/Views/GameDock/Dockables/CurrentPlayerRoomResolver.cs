using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

internal static class CurrentPlayerRoomResolver
{
    public static (bool HasRoom, short RoomId) Resolve(DecodedInGamePlayer[] players, byte localPlayerSlotIndex)
    {
        if (TryResolvePlayerRoom(players, localPlayerSlotIndex, out short roomId))
            return (true, roomId);

        if (TryResolveSharedTrackedRoom(players, out roomId))
            return (true, roomId);

        return (false, 0);
    }

    private static bool IsTrackedPlayer(DecodedInGamePlayer player) =>
        player.IsEnabled && player.IsInGame && (player.NameId > 0 || !string.IsNullOrEmpty(player.Type));

    private static bool TryResolvePlayerRoom(DecodedInGamePlayer[] players, byte localPlayerSlotIndex, out short roomId)
    {
        foreach (DecodedInGamePlayer player in players)
        {
            if (player.SlotIndex != localPlayerSlotIndex)
                continue;

            return TryGetPlayerRoom(player, out roomId);
        }

        if (localPlayerSlotIndex < players.Length)
            return TryGetPlayerRoom(players[localPlayerSlotIndex], out roomId);

        roomId = 0;
        return false;
    }

    private static bool TryResolveSharedTrackedRoom(DecodedInGamePlayer[] players, out short roomId)
    {
        bool hasCandidateRoom = false;
        short candidateRoomId = 0;

        foreach (DecodedInGamePlayer player in players)
        {
            if (!TryGetPlayerRoom(player, out short playerRoomId))
                continue;

            if (!hasCandidateRoom)
            {
                candidateRoomId = playerRoomId;
                hasCandidateRoom = true;
                continue;
            }

            if (candidateRoomId != playerRoomId)
            {
                roomId = 0;
                return false;
            }
        }

        roomId = candidateRoomId;
        return hasCandidateRoom;
    }

    private static bool TryGetPlayerRoom(DecodedInGamePlayer player, out short roomId)
    {
        if (!IsTrackedPlayer(player) || player.RoomId <= 0)
        {
            roomId = 0;
            return false;
        }

        roomId = player.RoomId;
        return true;
    }
}
