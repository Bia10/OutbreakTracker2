using System.Text.Json;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbyRoomReader : ReaderBase
{
    public LobbyRoomReader(GameClient gameClient, EEmemMemory eememMemory) : base(gameClient, eememMemory) { }

    public DecodedLobbyRoom[] DecodedLobbyRooms { get; } = new DecodedLobbyRoom[1];

    public short GetMaxPlayers()
    {
        nint[] offsets = GetOffsets([FileOnePtrs.LobbyRoomMaxPlayer], [FileTwoPtrs.LobbyRoomMaxPlayer]);
        nint address = ComputeAddress(offsets);
        short result = ReadValue<short>(address, nameof(GetMaxPlayers));

        return result;
    }

    public short GetDifficulty()
    {
        nint[] offsets = GetOffsets([FileOnePtrs.LobbyRoomDifficulty], [FileTwoPtrs.LobbyRoomDifficulty]);
        nint address = ComputeAddress(offsets);
        short result = ReadValue<short>(address, nameof(GetCurPlayers));

        return result;
    }

    public byte GetStatus()
    {
        nint[] offsets = GetOffsets([FileOnePtrs.LobbyRoomStatus], [FileTwoPtrs.LobbyRoomStatus]);
        nint address = ComputeAddress(offsets);
        byte result = ReadValue<byte>(address, nameof(GetCurPlayers));

        return result;
    }

    public short GetScenarioId()
    {
        nint[] offsets = GetOffsets([FileOnePtrs.LobbyRoomScenarioId], [FileTwoPtrs.LobbyRoomScenarioId]);
        nint address = ComputeAddress(offsets);
        short result = ReadValue<short>(address, nameof(GetCurPlayers));

        return result;
    }

    public short GetTime()
    {
        nint[] offsets = GetOffsets([FileOnePtrs.LobbyRoomTime], [FileTwoPtrs.LobbyRoomTime]);
        nint address = ComputeAddress(offsets);
        short result = ReadValue<short>(address, nameof(GetCurPlayers));

        return result;
    }

    public short GetCurPlayers()
    {
        nint[] offsets = GetOffsets([FileOnePtrs.LobbyRoomCurPlayer], [FileTwoPtrs.LobbyRoomCurPlayer]);
        nint address = ComputeAddress(offsets);
        short result = ReadValue<short>(address, nameof(GetCurPlayers));

        return result;
    }

    public string GetDifficultyString() => GetEnumString(GetDifficulty(), RoomDifficulty.Unknown);

    public string GetScenarioString()
    {
        short scenarioId = GetScenarioId();
        return CurrentFile switch
        {
            GameFile.FileOne => GetEnumString(scenarioId, FileOneScenario.Unknown),
            GameFile.FileTwo => GetEnumString(scenarioId, FileTwoScenario.Unknown),
            _ => throw new InvalidOperationException($"[{nameof(GetScenarioString)}] Unable to recognize current game file {CurrentFile.ToString()}")
        };
    }

    public string GetFormattedTimeString()
    {
        short time = GetTime();
        var timeSpan = TimeSpan.FromSeconds(time);
        var timeLeftStr = $"{timeSpan.Hours:D2}h:{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s:{timeSpan.Milliseconds:D3}ms";

        return timeLeftStr;
    }

    public void UpdateLobbyRoom(bool debug = false)
    {
        if (CurrentFile == GameFile.Unknown) return;

        for (var i = 0; i <= 1; i++)
        {
            Console.WriteLine($"Decoding lobby room {i}");

            DecodedLobbyRooms[i] = new DecodedLobbyRoom
            {
                MaxPlayer = GetMaxPlayers(),
                Difficulty = GetDifficultyString(),
                Status = GetStatus(),
                ScenarioName = GetScenarioString(),
                TimeLeft = GetFormattedTimeString(),
                CurPlayer = GetCurPlayers()
            };
        }

        if (!debug) return;

        foreach (DecodedLobbyRoom lobbyRoom in DecodedLobbyRooms)
            Console.WriteLine(JsonSerializer.Serialize(lobbyRoom, DecodedLobbyRoomJsonContext.Default.DecodedLobbyRoom));
    }
}