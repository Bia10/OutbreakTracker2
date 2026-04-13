using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.LobbyRoom;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class InGameScenarioReader(IGameClient gameClient, IEEmemAddressReader memory, ILogger logger)
    : ReaderBase(gameClient, memory, logger),
        IInGameScenarioReader
{
    public DecodedInGameScenario DecodedScenario { get; private set; } = new DecodedInGameScenario();

    private static string GetScenarioName(short scenarioId) => EnumUtility.GetEnumString(scenarioId, Scenario.Unknown);

    private static string GetDifficultyName(byte difficulty) =>
        EnumUtility.GetEnumString(difficulty, RoomDifficulty.Unknown);

    private static string GetItemTypeName(short typeId) => EnumUtility.GetEnumString(typeId, ItemType.Unknown);

    private short GetScenarioId() =>
        ReadValue(InGameScenarioOffsets.ScenarioId.File1, InGameScenarioOffsets.ScenarioId.File2, (short)-1);

    private int GetFrameCount() =>
        ReadValue(InGameScenarioOffsets.FrameCounter.File1, InGameScenarioOffsets.FrameCounter.File2, -1);

    private ScenarioStatus GetScenarioStatus() =>
        (ScenarioStatus)ReadValue(
            InGameScenarioOffsets.ScenarioStatus.File1,
            InGameScenarioOffsets.ScenarioStatus.File2,
            (byte)0xFF
        );

    private byte GetLocalPlayerSlotIndex() =>
        ReadValue(InGameScenarioOffsets.PlayerNumber.File1, InGameScenarioOffsets.PlayerNumber.File2, (byte)0xFF);

    private short GetWildThingsTime() =>
        ReadValue(InGameScenarioOffsets.WildThingsTime.File1, InGameScenarioOffsets.WildThingsTime.File2, (short)-1);

    private short GetFlashbackTime() =>
        ReadValue(InGameScenarioOffsets.FlashbackTime.File1, InGameScenarioOffsets.FlashbackTime.File2, (short)-1);

    private ushort GetWTGateMHp() =>
        ReadValue(InGameScenarioOffsets.WTGateMHp.File1, InGameScenarioOffsets.WTGateMHp.File2, (ushort)0);

    private ushort GetWTGateHp() =>
        ReadValue(InGameScenarioOffsets.WTGateHp.File1, InGameScenarioOffsets.WTGateHp.File2, (ushort)0);

    private short GetEscapeTime() =>
        ReadValue(InGameScenarioOffsets.EscapeTime.File1, InGameScenarioOffsets.EscapeTime.File2, (short)-1);

    private int GetDesperateTimesFightTime() =>
        ReadValue(
            InGameScenarioOffsets.DesperateTimesFightTime.File1,
            InGameScenarioOffsets.DesperateTimesFightTime.File2,
            (short)-1
        );

    private short GetDesperateTimesFightTime2() =>
        ReadValue(
            InGameScenarioOffsets.DesperateTimesFightTime2.File1,
            InGameScenarioOffsets.DesperateTimesFightTime2.File2,
            (short)-1
        );

    private int GetDesperateTimesGarageTime() =>
        ReadValue(
            InGameScenarioOffsets.DesperateTimesGarageTime.File1,
            InGameScenarioOffsets.DesperateTimesGarageTime.File2,
            (short)-1
        );

    private int GetDesperateTimesGasTime() =>
        ReadValue(
            InGameScenarioOffsets.DesperateTimesGasTime.File1,
            InGameScenarioOffsets.DesperateTimesGasTime.File2,
            (short)-1
        );

    private int GetDesperateTimesGasFlag() =>
        ReadValue(
            InGameScenarioOffsets.DesperateTimesGasFlag.File1,
            InGameScenarioOffsets.DesperateTimesGasFlag.File2,
            (short)-1
        );

    private byte GetDesperateTimesGasRandom() =>
        ReadValue(
            InGameScenarioOffsets.DesperateTimesGasRandom.File1,
            InGameScenarioOffsets.DesperateTimesGasRandom.File2,
            (byte)0xFF
        );

    private byte GetItemRandom() =>
        ReadValue(InGameScenarioOffsets.ItemRandom.File1, InGameScenarioOffsets.ItemRandom.File2, (byte)0xFF);

    private byte GetItemRandom2() =>
        ReadValue(InGameScenarioOffsets.ItemRandom2.File1, InGameScenarioOffsets.ItemRandom2.File2, (byte)0xFF);

    private byte GetPuzzleRandom() =>
        ReadValue(InGameScenarioOffsets.PuzzleRandom.File1, InGameScenarioOffsets.PuzzleRandom.File2, (byte)0xFF);

    private byte GetCoin()
    {
        nint basePtr = GetFileSpecificOffsets(InGameScenarioOffsets.Coin)[0];
        if (basePtr == nint.Zero)
            return 0xFF;

        byte coin1 = ReadValue<byte>(basePtr, [0x0]);
        byte coin2 = ReadValue<byte>(basePtr, [0x2]);
        byte coin3 = ReadValue<byte>(basePtr, [0x4]);
        byte coin4 = ReadValue<byte>(basePtr, [0x6]);

        return (byte)(coin1 + coin2 + coin3 + coin4);
    }

    private byte GetKilledZombies() =>
        ReadValue(InGameScenarioOffsets.KilledZombies.File1, InGameScenarioOffsets.KilledZombies.File2, (byte)0xFF);

    private byte GetPassWildThings() =>
        ReadValue(InGameScenarioOffsets.PassWildThings.File1, InGameScenarioOffsets.PassWildThings.File2, (byte)0xFF);

    private short GetPassDesperateTimes() =>
        ReadValue(
            InGameScenarioOffsets.PassDesperateTimes.File1,
            InGameScenarioOffsets.PassDesperateTimes.File2,
            (short)-1
        );

    private byte GetPassDesperateTimes2() =>
        ReadValue(
            InGameScenarioOffsets.PassDesperateTimes2.File1,
            InGameScenarioOffsets.PassDesperateTimes2.File2,
            (byte)0xFF
        );

    private byte GetPassDesperateTimes3() =>
        ReadValue(default, InGameScenarioOffsets.PassDesperateTimes3.File2, (byte)0xFF);

    private short GetPassUnderBelly1() => ReadValue(default, InGameScenarioOffsets.PassUnderBelly1.File2, (short)-1);

    private byte GetPassUnderBelly2() => ReadValue(default, InGameScenarioOffsets.PassUnderBelly2.File2, (byte)0xFF);

    private byte GetPassUnderBelly3() => ReadValue(default, InGameScenarioOffsets.PassUnderBelly3.File2, (byte)0xFF);

    private byte GetPass1() => ReadValue(InGameScenarioOffsets.Pass1.File1, default, (byte)0xFF);

    private byte GetPass2() => ReadValue(InGameScenarioOffsets.Pass2.File1, default, (byte)0xFF);

    private byte GetPass3() => ReadValue(InGameScenarioOffsets.Pass3.File1, default, (byte)0xFF);

    private short GetPass4() =>
        ReadValue(InGameScenarioOffsets.Pass4.File1, InGameScenarioOffsets.Pass4.File2, (short)-1);

    private byte GetPass5() => ReadValue(InGameScenarioOffsets.Pass5.File1, default, (byte)0xFF);

    private byte GetPass6() => ReadValue(InGameScenarioOffsets.Pass6.File1, default, (byte)0xFF);

    private byte GetDifficulty() =>
        ReadValue(InGameScenarioOffsets.Difficulty.File1, InGameScenarioOffsets.Difficulty.File2, (byte)0xFF);

    private DecodedItem[] GetItems()
    {
        DecodedItem[] items = new DecodedItem[GameConstants.MaxItems - 1];

        nint pickupSpaceStart = GetFileSpecificOffsets(InGameScenarioOffsets.PickupSpaceStart)[0];
        int pickupStructSize = GetFileSpecificSingleIntOffset(InGameScenarioOffsets.PickupStructSize);

        if (pickupSpaceStart == nint.Zero || pickupStructSize is 0)
        {
            Logger.LogWarning(
                "Failed to obtain valid pickup space start address or struct size. Returning empty items array."
            );
            return items;
        }

        nint roomIdOffset = GetFileSpecificSingleNintOffset(InGameScenarioOffsets.ItemRoomIdOffset);
        nint slotIndexOffset = GetFileSpecificSingleNintOffset(InGameScenarioOffsets.ItemNumberOffset);
        nint typeIdOffset = GetFileSpecificSingleNintOffset(InGameScenarioOffsets.ItemIdOffset);
        nint mixOffset = GetFileSpecificSingleNintOffset(InGameScenarioOffsets.ItemMixOffset);
        nint presentOffset = GetFileSpecificSingleNintOffset(InGameScenarioOffsets.ItemPresentOffset);
        nint quantityOffset = GetFileSpecificSingleNintOffset(InGameScenarioOffsets.ItemPickupCountOffset);
        nint pickedUpOffset = GetFileSpecificSingleNintOffset(InGameScenarioOffsets.ItemPickupOffset);

        for (int i = 0; i < GameConstants.MaxItems - 1; i++)
        {
            nint itemBaseOffset = (nint)pickupStructSize * i;
            short typeId = ReadValue<short>(pickupSpaceStart, [itemBaseOffset + typeIdOffset]);

            items[i] = new DecodedItem
            {
                Id = (short)(i + 1),
                RoomId = ReadValue<byte>(pickupSpaceStart, [itemBaseOffset + roomIdOffset]),
                SlotIndex = ReadValue<byte>(pickupSpaceStart, [itemBaseOffset + slotIndexOffset]),
                TypeId = typeId,
                TypeName = GetItemTypeName(typeId),
                Mix = ReadValue<byte>(pickupSpaceStart, [itemBaseOffset + mixOffset]),
                Present = ReadValue<int>(pickupSpaceStart, [itemBaseOffset + presentOffset]),
                Quantity = ReadValue<short>(pickupSpaceStart, [itemBaseOffset + quantityOffset]),
                PickedUp = ReadValue<short>(pickupSpaceStart, [itemBaseOffset + pickedUpOffset]),
            };
        }

        return items;
    }

    // Keep the core scenario poll alive for any non-None session state so the application
    // still receives transition and terminal status updates. Live gameplay visibility/data
    // is gated later from the published ScenarioStatus value.
    public bool IsInScenario()
    {
        ScenarioStatus status = GetScenarioStatus();
        int frameCount = GetFrameCount();

        return frameCount > 0 && status is not ScenarioStatus.None and not (ScenarioStatus)0xFF;
    }

    public void UpdateScenario()
    {
        if (CurrentFile is GameFile.Unknown)
            return;

        byte localPlayerSlotIndex = GetLocalPlayerSlotIndex();

        DecodedScenario = new DecodedInGameScenario
        {
            CurrentFile = (byte)CurrentFile,
            ScenarioName = GetScenarioName(GetScenarioId()),
            FrameCounter = GetFrameCount(),
            Status = GetScenarioStatus(),
            PlayerCount = localPlayerSlotIndex,
            LocalPlayerSlotIndex = localPlayerSlotIndex,
            WildThingsTime = CurrentFile == GameFile.FileTwo ? GetWildThingsTime() : (short)-1,
            FlashbackTime = CurrentFile == GameFile.FileTwo ? GetFlashbackTime() : (short)-1,
            WTGateMHp = CurrentFile == GameFile.FileTwo ? GetWTGateMHp() : (ushort)0,
            WTGateHp = CurrentFile == GameFile.FileTwo ? GetWTGateHp() : (ushort)0,
            EscapeTime = CurrentFile == GameFile.FileTwo ? GetEscapeTime() : (short)-1,
            FightTime = CurrentFile == GameFile.FileTwo ? GetDesperateTimesFightTime() : -1,
            FightTime2 = CurrentFile == GameFile.FileTwo ? GetDesperateTimesFightTime2() : (short)-1,
            GarageTime = CurrentFile == GameFile.FileTwo ? GetDesperateTimesGarageTime() : -1,
            GasTime = CurrentFile == GameFile.FileTwo ? GetDesperateTimesGasTime() : -1,
            GasFlag = CurrentFile == GameFile.FileTwo ? GetDesperateTimesGasFlag() : -1,
            GasRandom = CurrentFile == GameFile.FileTwo ? GetDesperateTimesGasRandom() : (byte)0xFF,
            ItemRandom = GetItemRandom(),
            ItemRandom2 = GetItemRandom2(),
            PuzzleRandom = GetPuzzleRandom(),
            Coin = CurrentFile == GameFile.FileTwo ? GetCoin() : (byte)0xFF,
            KilledZombie = CurrentFile == GameFile.FileTwo ? GetKilledZombies() : (byte)0xFF,
            PassWildThings = CurrentFile == GameFile.FileTwo ? GetPassWildThings() : (byte)0xFF,
            PassDesperateTimes1 = CurrentFile == GameFile.FileTwo ? GetPassDesperateTimes() : (short)-1,
            PassDesperateTimes2 = CurrentFile == GameFile.FileTwo ? GetPassDesperateTimes2() : (byte)0xFF,
            PassDesperateTimes3 = CurrentFile == GameFile.FileTwo ? GetPassDesperateTimes3() : (byte)0xFF,
            Pass1 = CurrentFile == GameFile.FileOne ? GetPass1() : (byte)0,
            Pass2 = CurrentFile == GameFile.FileOne ? GetPass2() : (byte)0,
            Pass3 = CurrentFile == GameFile.FileOne ? GetPass3() : (byte)0,
            PassUnderbelly1 = CurrentFile == GameFile.FileTwo ? GetPassUnderBelly1() : (short)-1,
            PassUnderbelly2 = CurrentFile == GameFile.FileTwo ? GetPassUnderBelly2() : (byte)0xFF,
            PassUnderbelly3 = CurrentFile == GameFile.FileTwo ? GetPassUnderBelly3() : (byte)0xFF,
            Pass4 = GetPass4(),
            Pass5 = CurrentFile == GameFile.FileOne ? GetPass5() : (byte)0,
            Pass6 = CurrentFile == GameFile.FileOne ? GetPass6() : (byte)0,
            Difficulty = GetDifficultyName(GetDifficulty()),
            Items = GetItems(),
        };
    }
}
