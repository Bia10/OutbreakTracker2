using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.LobbyRoom;
using OutbreakTracker2.Outbreak.Enums.LobbySlot;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbyRoomReader(IGameClient gameClient, IEEmemAddressReader eememMemory, ILogger logger)
    : ReaderBase(gameClient, eememMemory, logger),
        ILobbyRoomReader
{
    public DecodedLobbyRoom DecodedLobbyRoom { get; private set; } = new DecodedLobbyRoom();

    private short GetCurPlayers() =>
        ReadValue(LobbyRoomOffsets.CurPlayers.File1, LobbyRoomOffsets.CurPlayers.File2, (short)-1);

    private short GetMaxPlayers() =>
        ReadValue(LobbyRoomOffsets.MaxPlayers.File1, LobbyRoomOffsets.MaxPlayers.File2, (short)-1);

    private short GetDifficulty() =>
        ReadValue(LobbyRoomOffsets.Difficulty.File1, LobbyRoomOffsets.Difficulty.File2, (short)-1);

    private byte GetStatus() => ReadValue(LobbyRoomOffsets.Status.File1, LobbyRoomOffsets.Status.File2, (byte)0xFF);

    private short GetScenarioId() =>
        ReadValue(LobbyRoomOffsets.ScenarioId.File1, LobbyRoomOffsets.ScenarioId.File2, (short)-1);

    private short GetTime() => ReadValue(LobbyRoomOffsets.Time.File1, LobbyRoomOffsets.Time.File2, (short)-1);

    private static string GetDifficultyName(short difficulty) =>
        EnumUtility.GetEnumString(difficulty, RoomDifficulty.Unknown);

    private static string GetStatusName(byte status)
    {
        string result = EnumUtility.GetEnumString(status, RoomStatus.Unknown);
        return result.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? $"{result}({status})" : result;
    }

    private string GetScenarioName(short scenarioId) =>
        GetScenarioString(scenarioId, FileOneLobbyScenario.Unknown, FileTwoLobbyScenario.Unknown);

    private string GetFormattedTimeString()
    {
        try
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(GetTime());
            return $"{timeSpan.Hours:D2}h:{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting time");
            return "Invalid time format";
        }
    }

    public void UpdateLobbyRoom()
    {
        if (CurrentFile is GameFile.Unknown)
            return;

        DecodedLobbyRoom = new DecodedLobbyRoom
        {
            CurPlayer = GetCurPlayers(),
            MaxPlayer = GetMaxPlayersCount(GetMaxPlayers()),
            Difficulty = GetDifficultyName(GetDifficulty()),
            Status = GetStatusName(GetStatus()),
            ScenarioName = GetScenarioName(GetScenarioId()),
            TimeLeft = GetFormattedTimeString(),
        };
    }

    private static short GetMaxPlayersCount(short rawValue) =>
        (RoomMaxPlayers)rawValue switch
        {
            RoomMaxPlayers.Two => 2,
            RoomMaxPlayers.Three => 3,
            RoomMaxPlayers.Four => 4,
            _ => 4,
        };
}
