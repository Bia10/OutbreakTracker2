using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.LobbySlot;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbySlotReader : ReaderBase
{
    public DecodedLobbySlot[] DecodedLobbySlots { get; private set; }

    public LobbySlotReader(GameClient gameClient, IEEmemMemory memory, ILogger logger) : base(
        gameClient, memory, logger)
    {
        DecodedLobbySlots = new DecodedLobbySlot[GameConstants.MaxLobbySlots];
        for (int i = 0; i < GameConstants.MaxLobbySlots; i++)
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

    public static string GetStatusName(byte status)
        => EnumUtility.GetEnumString(status, SlotStatus.Unknown);

    public static string GetPassName(byte pass)
        => EnumUtility.GetEnumString(pass, SlotPass.NoPass);

    public static string GetVersionName(short version)
        => EnumUtility.GetEnumString(version, GameVersion.Unknown);

    public string GetScenarioName(short scenarioId)
        => GetScenarioString(scenarioId, FileOneLobbyScenario.Unknown, FileTwoLobbyScenario.Unknown);

    public void UpdateLobbySlots(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;
        List<string> errors = [];

        DecodedLobbySlot[] newLobbySlotsData = new DecodedLobbySlot[GameConstants.MaxLobbySlots];

        for (int i = 0; i < GameConstants.MaxLobbySlots; i++)
        {
            if (debug) Logger.LogDebug("Decoding lobby at slot index: {SlotIndex}", i);

            byte passStatus = GetPass(i);
            Logger.LogDebug("Decoding lobby at slot index: {SlotIndex} current pass status: {PassStatus}", i, passStatus);

            try
            {
                newLobbySlotsData[i] = new DecodedLobbySlot
                {
                    SlotNumber = GetIndex(i),
                    CurPlayers = GetCurPlayers(i),
                    MaxPlayers = GetMaxPlayers(i),
                    Status = GetStatusName(GetStatus(i)),
                    IsPassProtected = GetPassName(GetPass(i)),
                    ScenarioId = GetScenarioName(GetScenarioId(i)),
                    Version = GetVersionName(GetVersion(i)),
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
            for (int i = 0; i < errors.Count; i++)
                Logger.LogError("Error {I}: {Error}", i, errors[i]);
        }

        DecodedLobbySlots = newLobbySlotsData;

        long duration = Environment.TickCount64 - start;
        if (!debug) return;

        Logger.LogDebug("Decoded lobby slots in {Duration}ms", duration);
        foreach (string jsonObject in DecodedLobbySlots.Select(lobbySlot
                     => JsonSerializer.Serialize(lobbySlot, DecodedLobbySlotJsonContext.Default.DecodedLobbySlot)))
            Logger.LogDebug("Decoded lobby slot: {JsonObject}", jsonObject);
    }
}