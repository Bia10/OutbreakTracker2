using System.Text.Json;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbySlotReader : ReaderBase
{
    public DecodedLobbySlot[] DecodedLobbySlots { get; }

    public LobbySlotReader(GameClient gameClient, EEmemMemory memory, ILogger logger) : base(
        gameClient, memory, logger)
    {
        DecodedLobbySlots = new DecodedLobbySlot[Constants.MaxLobbySlots];
        for (int i = 0; i < Constants.MaxLobbySlots; i++)
            DecodedLobbySlots[i] = new DecodedLobbySlot();
    }

    public short GetIndex(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.Index, (short)-1);

    public short GetCurPlayers(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.CurPlayers, (short)-1);

    public short GetMaxPlayers(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.MaxPlayers, (short)-1);

    public byte GetStatus(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.Status, (byte)SlotStatus.Unknown);

    public byte GetPass(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.Pass, (byte)SlotPass.NoPass);

    public short GetScenarioId(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.ScenarioId, (short)FileTwoLobbyScenario.Unknown);

    public short GetVersion(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.Version, (short)GameVersion.Unknown);

    public string GetTitle(int slotIndex)
        => ReadSlotString(slotIndex, LobbySlotOffsets.Title, string.Empty);

    public string GetStatusString(int slotIndex)
        => GetEnumString(GetStatus(slotIndex), SlotStatus.Unknown);

    public string GetPassString(int slotIndex)
        => GetEnumString(GetPass(slotIndex), SlotPass.NoPass);

    public string GetVersionString(int slotIndex)
        => GetEnumString(GetVersion(slotIndex), GameVersion.Unknown);

    public string GetScenarioString(int slotIndex)
        => GetScenarioString(GetScenarioId(slotIndex), FileOneLobbyScenario.Unknown, FileTwoLobbyScenario.Unknown);

    public void UpdateLobbySlots(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;
        var errors = new List<string>();

        for (var i = 0; i < Constants.MaxLobbySlots; i++)
        {
            if (debug) Logger.LogDebug("Decoding lobby at slot index: {SlotIndex}", i);

            try
            {
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
            catch (Exception ex)
            {
                errors.Add($"Slot {i} error: {ex.Message}");
                Logger.LogError(ex, "Error decoding slot {SlotIndex}", i);
            }
        }

        if (errors.Count > 0)
        {
            Logger.LogError("Encountered {ErrorCount} errors", errors.Count);
            foreach (string error in errors)
                Logger.LogError(error);
        }

        long duration = Environment.TickCount64 - start;
        if (!debug) return;

        Logger.LogDebug("Decoded lobby slots in {Duration}ms", duration);

        foreach (DecodedLobbySlot lobbySlot in DecodedLobbySlots)
            Console.WriteLine(JsonSerializer.Serialize(lobbySlot, DecodedLobbySlotJsonContext.Default.DecodedLobbySlot));
    }
}