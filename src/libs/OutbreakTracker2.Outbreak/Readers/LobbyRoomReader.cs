using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbyRoomReader : ReaderBase
{
    public LobbyRoomReader(GameClient gameClient, EEmemMemory eememMemory) 
        : base(gameClient, eememMemory) { }

    public DecodedLobbyRoom[] DecodedLobbyRooms { get; } = new DecodedLobbyRoom[1];

    public short GetMaxPlayers()
        => ReadValue(LobbyRoomOffsets.MaxPlayers, nameof(GetMaxPlayers), (short)-1);

    public short GetDifficulty()
        => ReadValue(LobbyRoomOffsets.Difficulty, nameof(GetDifficulty), (short)-1);

    public byte GetStatus()
        => ReadValue(LobbyRoomOffsets.Status, nameof(GetStatus), (byte)0xFF);

    public short GetScenarioId()
        => ReadValue(LobbyRoomOffsets.ScenarioId, nameof(GetScenarioId), (short)-1);

    public short GetTime()
        => ReadValue(LobbyRoomOffsets.Time, nameof(GetTime), (short)-1);

    public short GetCurPlayers()
        => ReadValue(LobbyRoomOffsets.CurPlayers, nameof(GetCurPlayers), (short)-1);

    public string GetDifficultyString()
        => GetEnumString(GetDifficulty(), RoomDifficulty.Unknown);

    public string GetScenarioString()
        => GetScenarioString(GetScenarioId(), 
            FileOneScenario.Unknown, 
            FileTwoScenario.Unknown);

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

        if (debug) Console.WriteLine("Decoding lobby room");

        DecodedLobbyRooms[0] = new DecodedLobbyRoom
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

        Console.WriteLine($"Decoded lobby room in {duration}ms");

        Console.WriteLine(JsonSerializer.Serialize(
            DecodedLobbyRooms[0], 
            DecodedLobbyRoomJsonContext.Default.DecodedLobbyRoom));
    }
}