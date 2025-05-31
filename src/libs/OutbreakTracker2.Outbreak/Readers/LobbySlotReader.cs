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

    public LobbySlotReader(GameClient gameClient, IEEmemMemory memory, ILogger logger)
        : base(gameClient, memory, logger)
    {
        DecodedLobbySlots = new DecodedLobbySlot[GameConstants.MaxLobbySlots];
        for (int i = 0; i < GameConstants.MaxLobbySlots; i++)
            DecodedLobbySlots[i] = new DecodedLobbySlot();
    }

    private short GetIndex(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.Index.File1, LobbySlotOffsets.Index.File2, (short)-1);

    private short GetCurPlayers(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.CurPlayers.File1, LobbySlotOffsets.CurPlayers.File2, (short)-1);

    private short GetMaxPlayers(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.MaxPlayers.File1, LobbySlotOffsets.MaxPlayers.File2, (short)-1);

    private byte GetStatus(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.Status.File1, LobbySlotOffsets.Status.File2, (byte)SlotStatus.Unknown);

    private byte GetPass(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.Pass.File1, LobbySlotOffsets.Pass.File2, (byte)255);

    private short GetScenarioId(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.ScenarioId.File1, LobbySlotOffsets.ScenarioId.File2, (short)FileTwoLobbyScenario.Unknown);

    private short GetVersion(int slotIndex)
        => ReadSlotValue(slotIndex, LobbySlotOffsets.Version.File1, LobbySlotOffsets.Version.File2, (short)-1);

    private string GetTitle(int slotIndex)
        => ReadSlotString(slotIndex, LobbySlotOffsets.Title.File1, LobbySlotOffsets.Title.File2, string.Empty);

    private static string GetStatusName(byte status)
        => EnumUtility.GetEnumString(status, SlotStatus.Unknown);

    private static string GetPassName(byte pass)
        => EnumUtility.GetEnumString(pass, SlotPass.NoPass);

    private static string GetVersionName(short version)
        => EnumUtility.GetEnumString(version, GameVersion.Unknown);

    private string GetScenarioName(short scenarioId)
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