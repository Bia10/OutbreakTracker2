using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.LobbySlot;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbySlotReader : ReaderBase, ILobbySlotReader
{
    public DecodedLobbySlot[] DecodedLobbySlots { get; private set; }

    public LobbySlotReader(IGameClient gameClient, IEEmemAddressReader memory, ILogger logger)
        : base(gameClient, memory, logger)
    {
        DecodedLobbySlots = new DecodedLobbySlot[GameConstants.MaxLobbySlots];
        for (int i = 0; i < GameConstants.MaxLobbySlots; i++)
            DecodedLobbySlots[i] = new DecodedLobbySlot();
    }

    private short GetIndex(int slotIndex) =>
        ReadSlotValue(slotIndex, LobbySlotOffsets.Index.File1, LobbySlotOffsets.Index.File2, (short)-1);

    private short GetCurPlayers(int slotIndex) =>
        ReadSlotValue(slotIndex, LobbySlotOffsets.CurPlayers.File1, LobbySlotOffsets.CurPlayers.File2, (short)-1);

    private short GetMaxPlayers(int slotIndex) =>
        ReadSlotValue(slotIndex, LobbySlotOffsets.MaxPlayers.File1, LobbySlotOffsets.MaxPlayers.File2, (short)-1);

    private byte GetStatus(int slotIndex) =>
        ReadSlotValue(
            slotIndex,
            LobbySlotOffsets.Status.File1,
            LobbySlotOffsets.Status.File2,
            (byte)SlotStatus.Unknown
        );

    private byte GetPass(int slotIndex) =>
        ReadSlotValue(slotIndex, LobbySlotOffsets.Pass.File1, LobbySlotOffsets.Pass.File2, (byte)255);

    private short GetScenarioId(int slotIndex) =>
        ReadSlotValue(
            slotIndex,
            LobbySlotOffsets.ScenarioId.File1,
            LobbySlotOffsets.ScenarioId.File2,
            (short)FileTwoLobbyScenario.Unknown
        );

    private short GetVersion(int slotIndex) =>
        ReadSlotValue(slotIndex, LobbySlotOffsets.Version.File1, LobbySlotOffsets.Version.File2, (short)-1);

    private string GetTitle(int slotIndex) =>
        ReadSlotString(slotIndex, LobbySlotOffsets.Title.File1, LobbySlotOffsets.Title.File2, string.Empty);

    private static string GetStatusName(byte status) => EnumUtility.GetEnumString(status, SlotStatus.Unknown);

    private static string GetVersionName(short version) => EnumUtility.GetEnumString(version, GameVersion.Unknown);

    private string GetScenarioName(short scenarioId) =>
        GetScenarioString(scenarioId, FileOneLobbyScenario.Unknown, FileTwoLobbyScenario.Unknown);

    public void UpdateLobbySlots()
    {
        if (CurrentFile is GameFile.Unknown)
            return;

        DecodedLobbySlot[] newLobbySlotsData = new DecodedLobbySlot[GameConstants.MaxLobbySlots];

        for (int i = 0; i < GameConstants.MaxLobbySlots; i++)
        {
            try
            {
                newLobbySlotsData[i] = new DecodedLobbySlot
                {
                    Id = GetPersistentUlidForLobbySlot(i),
                    SlotNumber = GetIndex(i),
                    CurPlayers = GetCurPlayers(i),
                    MaxPlayers = GetMaxPlayers(i),
                    Status = GetStatusName(GetStatus(i)),
                    IsPassProtected = GetPass(i) != 0,
                    ScenarioId = GetScenarioName(GetScenarioId(i)),
                    Version = GetVersionName(GetVersion(i)),
                    Title = GetTitle(i),
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error decoding slot {SlotIndex}", i);
            }
        }

        DecodedLobbySlots = newLobbySlotsData;
    }

    private readonly Dictionary<int, Ulid> _lobbySlotUlids = [];

    private Ulid GetPersistentUlidForLobbySlot(int slotIndex)
    {
        if (_lobbySlotUlids.TryGetValue(slotIndex, out Ulid ulid))
            return ulid;

        ulid = Ulid.NewUlid();
        _lobbySlotUlids.Add(slotIndex, ulid);

        return ulid;
    }
}
