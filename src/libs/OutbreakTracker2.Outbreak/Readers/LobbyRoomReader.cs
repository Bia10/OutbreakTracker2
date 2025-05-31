using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.LobbyRoom;
using OutbreakTracker2.Outbreak.Enums.LobbySlot;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbyRoomReader : ReaderBase
{
    public DecodedLobbyRoom DecodedLobbyRoom { get; private set; }

    public LobbyRoomReader(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedLobbyRoom = new DecodedLobbyRoom();
    }

    private short GetCurPlayers()
        => ReadValue(LobbyRoomOffsets.CurPlayers.File1, LobbyRoomOffsets.CurPlayers.File2, (short)-1);

    private short GetMaxPlayers()
        => ReadValue(LobbyRoomOffsets.MaxPlayers.File1, LobbyRoomOffsets.MaxPlayers.File2, (short)-1);

    private short GetDifficulty()
        => ReadValue(LobbyRoomOffsets.Difficulty.File1, LobbyRoomOffsets.Difficulty.File2, (short)-1);

    private byte GetStatus()
        => ReadValue(LobbyRoomOffsets.Status.File1, LobbyRoomOffsets.Status.File2, (byte)0xFF);

    private short GetScenarioId()
        => ReadValue(LobbyRoomOffsets.ScenarioId.File1, LobbyRoomOffsets.ScenarioId.File2, (short)-1);

    private short GetTime()
        => ReadValue(LobbyRoomOffsets.Time.File1, LobbyRoomOffsets.Time.File2, (short)-1);

    private static string GetMaxPlayersString(short maxPlayers)
        => EnumUtility.GetEnumString(maxPlayers, RoomMaxPlayers.Two);

    private static string GetDifficultyName(short difficulty)
        => EnumUtility.GetEnumString(difficulty, RoomDifficulty.Unknown);

    private static string GetStatusName(byte status)
    {
        string result = EnumUtility.GetEnumString(status, RoomStatus.Unknown);
        return result.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? $"{result}({status})" : result;
    }

    private string GetScenarioName(short scenarioId)
        => GetScenarioString(scenarioId,
            FileOneLobbyScenario.Unknown,
            FileTwoLobbyScenario.Unknown);

    private string GetFormattedTimeString()
    {
        try
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(GetTime());
            return $"{timeSpan.Hours:D2}h:{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s";
        }
        catch
        {
            return "Invalid time format";
        }
    }

    public void UpdateLobbyRoom(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Logger.LogDebug("Decoding lobby room");

        DecodedLobbyRoom = new DecodedLobbyRoom
        {
            CurPlayer = GetCurPlayers(),
            MaxPlayer = short.Parse(GetMaxPlayersString(GetMaxPlayers())),
            Difficulty = GetDifficultyName(GetDifficulty()),
            Status = GetStatusName(GetStatus()),
            ScenarioName = GetScenarioName(GetScenarioId()),
            TimeLeft = GetFormattedTimeString()
        };

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded lobby room in {Duration}ms", duration);
        string jsonObject = JsonSerializer.Serialize(DecodedLobbyRoom, DecodedLobbyRoomJsonContext.Default.DecodedLobbyRoom);
        Logger.LogDebug("Decoded lobby room: {JsonObject}", jsonObject);
    }
}