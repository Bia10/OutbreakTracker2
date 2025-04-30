using System.Text.Json;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbyRoomReader : ReaderBase
{
    public DecodedLobbyRoom DecodedLobbyRoom { get; set; }

    public LobbyRoomReader(GameClient gameClient, EEmemMemory eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedLobbyRoom = new DecodedLobbyRoom();
    }

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

    public short GetCurPlayers()
        => ReadValue(LobbyRoomOffsets.CurPlayers, (short)-1);

    public string GetDifficultyString()
        => GetEnumString(GetDifficulty(), RoomDifficulty.Unknown);

    public string GetScenarioString()
        => GetScenarioString(GetScenarioId(),
            FileOneLobbyScenario.Unknown,
            FileTwoLobbyScenario.Unknown);

    public string GetFormattedTimeString()
    {
        try
        {
            var timeSpan = TimeSpan.FromSeconds(GetTime());
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
            MaxPlayer = GetMaxPlayers(),
            Difficulty = GetDifficultyString(),
            Status = GetStatus(),
            ScenarioName = GetScenarioString(),
            TimeLeft = GetFormattedTimeString(),
            CurPlayer = GetCurPlayers()
        };

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded lobby room in {Duration}ms", duration);
        Logger.LogDebug(JsonSerializer.Serialize(
            DecodedLobbyRoom,
            DecodedLobbyRoomJsonContext.Default.DecodedLobbyRoom));
    }
}