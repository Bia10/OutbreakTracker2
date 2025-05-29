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
    public DecodedLobbyRoom DecodedLobbyRoom { get; set; }

    public LobbyRoomReader(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedLobbyRoom = new DecodedLobbyRoom();
    }

    public short GetCurPlayers()
        => ReadValue(LobbyRoomOffsets.CurPlayers, (short)-1);

    // TODO: this seems to return wrong data
    public short GetMaxPlayers()
        => ReadValue(LobbyRoomOffsets.MaxPlayers, (short)-1);

    public short GetDifficulty()
        => ReadValue(LobbyRoomOffsets.Difficulty, (short)-1);

    public byte GetStatus()
        => ReadValue(LobbyRoomOffsets.Status, (byte)0xFF);

    public short GetScenarioId()
        => ReadValue(LobbyRoomOffsets.ScenarioId, (short)-1);

    public short GetTime()
        => ReadValue(LobbyRoomOffsets.Time, (short)-1);

    public static string GetDifficultyName(short difficulty)
        => EnumUtility.GetEnumString(difficulty, RoomDifficulty.Unknown);

    public static string GetStatusName(byte status)
        => EnumUtility.GetEnumString(status, RoomStatus.Unknown);

    public string GetScenarioName(short scenarioId)
        => GetScenarioString(scenarioId,
            FileOneLobbyScenario.Unknown,
            FileTwoLobbyScenario.Unknown);

    public string GetFormattedTimeString()
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
            MaxPlayer = GetMaxPlayers(),
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