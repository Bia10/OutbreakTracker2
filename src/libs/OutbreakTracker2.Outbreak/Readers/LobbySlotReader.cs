using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;
using System.Text.Json;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbySlotReader : ReaderBase
{
    public LobbySlotReader(GameClient gameClient, EEmemMemory memory) : base(gameClient, memory) { }

    public DecodedLobbySlot[] DecodedLobbySlots { get; } = new DecodedLobbySlot[Constants.MaxLobbySlots];

    public short GetIndex(int slotIndex) => ReadSlotValue(slotIndex, [], [], nameof(GetIndex), (short)-1);
    public short GetCurPlayers(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotPlayer], [FileTwoPtrs.LobbySlotPlayer], nameof(GetCurPlayers), (short)-1);
    public short GetMaxPlayers(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotMaxPlayer], [FileTwoPtrs.LobbySlotMaxPlayer], nameof(GetMaxPlayers), (short)-1);
    public byte GetStatus(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotStatus], [FileTwoPtrs.LobbySlotStatus], nameof(GetStatus), (byte)SlotStatus.Unknown);
    public byte GetPass(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotPass], [FileTwoPtrs.LobbySlotPass], nameof(GetPass), (byte)SlotPass.NoPass);
    public short GetScenarioId(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotScenarioID], [FileTwoPtrs.LobbySlotScenarioID], nameof(GetScenarioId), (short)FileTwoScenario.Unknown);
    public short GetVersion(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotVersion], [FileTwoPtrs.LobbySlotVersion], nameof(GetVersion), (short)GameVersion.Unknown);
    public string GetTitle(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotTitle], [FileTwoPtrs.LobbySlotTitle], nameof(GetTitle), string.Empty);

    public string GetStatusString(int slotIndex) => GetEnumString(GetStatus(slotIndex), SlotStatus.Unknown);
    public string GetPassString(int slotIndex) => GetEnumString(GetPass(slotIndex), SlotPass.NoPass);
    public string GetVersionString(int slotIndex) => GetEnumString(GetVersion(slotIndex), GameVersion.Unknown);

    public string GetScenarioString(int slotIndex)
    {
        short scenarioId = GetScenarioId(slotIndex);
        return CurrentFile switch
        {
            GameFile.FileOne => GetEnumString(scenarioId, FileOneScenario.Unknown),
            GameFile.FileTwo => GetEnumString(scenarioId, FileTwoScenario.Unknown),
            _ => throw new InvalidOperationException($"[{nameof(GetScenarioString)}] Unable to recognize current game file {CurrentFile.ToString()}")
        };
    }

    public void UpdateLobbySlots(bool debug = false)
    {
        if (CurrentFile == GameFile.Unknown) return;

        for (var i = 0; i < Constants.MaxLobbySlots; i++)
        {
            Console.WriteLine($"Decoding lobby at slot index: {i}");

            DecodedLobbySlots[i] = new DecodedLobbySlot
            {
                SlotNumber = GetIndex(i),
                CurPlayers = GetCurPlayers(i),
                MaxPlayers = GetMaxPlayers(i),
                Status = GetStatusString(i),
                IsPassProtected = GetPassString(i),
                ScenarioId = GetScenarioString(i),
                Version = GetVersionString(i),
                Title = GetTitle(i)
            };
        }

        if (!debug) return;

        foreach (DecodedLobbySlot lobbySlot in DecodedLobbySlots)
            Console.WriteLine(JsonSerializer.Serialize(lobbySlot, DecodedLobbySlotJsonContext.Default.DecodedLobbySlot));
    }
}