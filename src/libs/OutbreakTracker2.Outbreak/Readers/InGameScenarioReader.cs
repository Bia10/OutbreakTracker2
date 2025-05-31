using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.LobbyRoom;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class InGameScenarioReader : ReaderBase
{
    public DecodedInGameScenario DecodedScenario { get; private set; }

    public InGameScenarioReader(GameClient gameClient, IEEmemMemory memory, ILogger logger)
        : base(gameClient, memory, logger)
    {
        DecodedScenario = new DecodedInGameScenario();
    }

    private static string GetScenarioName(short scenarioId)
        => EnumUtility.GetEnumString(scenarioId, Scenario.Unknown);

    private static string GetDifficultyName(byte difficulty)
        => EnumUtility.GetEnumString(difficulty, RoomDifficulty.Unknown);

    private static string GetItemTypeName(short typeId)
        => EnumUtility.GetEnumString(typeId, ItemType.Unknown);

    private short GetScenarioId()
        => ReadValue(InGameScenarioOffsets.ScenarioId.File1, InGameScenarioOffsets.ScenarioId.File2, (short)-1);

    private int GetFrameCount()
        => ReadValue(InGameScenarioOffsets.FrameCounter.File1, InGameScenarioOffsets.FrameCounter.File2, -1);

    private byte GetScenarioStatus()
        => ReadValue(InGameScenarioOffsets.ScenarioStatus.File1, InGameScenarioOffsets.ScenarioStatus.File2, (byte)0xFF);

    private byte GetPlayerCount()
        => ReadValue(InGameScenarioOffsets.PlayerNumber.File1, InGameScenarioOffsets.PlayerNumber.File2, (byte)0xFF);

    private short GetWildThingsTime()
        => ReadValue(InGameScenarioOffsets.WildThingsTime.File1, InGameScenarioOffsets.WildThingsTime.File2, (short)-1);

    private short GetEscapeTime()
        => ReadValue(InGameScenarioOffsets.EscapeTime.File1, InGameScenarioOffsets.EscapeTime.File2, (short)-1);

    private int GetDesperateTimesFightTime()
        => ReadValue(InGameScenarioOffsets.DesperateTimesFightTime.File1, InGameScenarioOffsets.DesperateTimesFightTime.File2, (short)-1);

    private short GetDesperateTimesFightTime2()
        => ReadValue(InGameScenarioOffsets.DesperateTimesFightTime2.File1, InGameScenarioOffsets.DesperateTimesFightTime2.File2, (short)-1);

    private int GetDesperateTimesGarageTime()
        => ReadValue(InGameScenarioOffsets.DesperateTimesGarageTime.File1, InGameScenarioOffsets.DesperateTimesGarageTime.File2, (short)-1);

    private int GetDesperateTimesGasTime()
        => ReadValue(InGameScenarioOffsets.DesperateTimesGasTime.File1, InGameScenarioOffsets.DesperateTimesGasTime.File2, (short)-1);

    private int GetDesperateTimesGasFlag()
        => ReadValue(InGameScenarioOffsets.DesperateTimesGasFlag.File1, InGameScenarioOffsets.DesperateTimesGasFlag.File2, (short)-1);

    private byte GetDesperateTimesGasRandom()
        => ReadValue(InGameScenarioOffsets.DesperateTimesGasRandom.File1, InGameScenarioOffsets.DesperateTimesGasRandom.File2, (byte)0xFF);

    private byte GetItemRandom()
        => ReadValue(InGameScenarioOffsets.ItemRandom.File1, InGameScenarioOffsets.ItemRandom.File2, (byte)0xFF);

    private byte GetItemRandom2()
        => ReadValue(InGameScenarioOffsets.ItemRandom2.File1, InGameScenarioOffsets.ItemRandom2.File2, (byte)0xFF);

    private byte GetPuzzleRandom()
        => ReadValue(InGameScenarioOffsets.PuzzleRandom.File1, InGameScenarioOffsets.PuzzleRandom.File2, (byte)0xFF);

    private byte GetCoin()
    {
        nint basePtr = GetFileSpecificOffsets(InGameScenarioOffsets.Coin)[0];
        if (basePtr == nint.Zero) return 0xFF;

        byte coin1 = ReadValue<byte>(basePtr, [0x0]);
        byte coin2 = ReadValue<byte>(basePtr, [0x2]);
        byte coin3 = ReadValue<byte>(basePtr, [0x4]);
        byte coin4 = ReadValue<byte>(basePtr, [0x6]);

        return (byte)(coin1 + coin2 + coin3 + coin4);
    }

    private byte GetKilledZombies()
        => ReadValue(InGameScenarioOffsets.KilledZombies.File1, InGameScenarioOffsets.KilledZombies.File2, (byte)0xFF);

    private byte GetPassWildThings()
        => ReadValue(InGameScenarioOffsets.PassWildThings.File1, InGameScenarioOffsets.PassWildThings.File2, (byte)0xFF);

    private short GetPassDesperateTimes()
        => ReadValue(InGameScenarioOffsets.PassDesperateTimes.File1, InGameScenarioOffsets.PassDesperateTimes.File2, (short)-1);

    private byte GetPassDesperateTimes2()
        => ReadValue(InGameScenarioOffsets.PassDesperateTimes2.File1, InGameScenarioOffsets.PassDesperateTimes2.File2, (byte)0xFF);

    private byte GetPassDesperateTimes3()
        => ReadValue(default, InGameScenarioOffsets.PassDesperateTimes3.File2, (byte)0xFF);

    private short GetPassUnderBelly1()
        => ReadValue(default, InGameScenarioOffsets.PassUnderBelly1.File2, (short)-1);

    private byte GetPassUnderBelly2()
        => ReadValue(default, InGameScenarioOffsets.PassUnderBelly2.File2, (byte)0xFF);

    private byte GetPassUnderBelly3()
        => ReadValue(default, InGameScenarioOffsets.PassUnderBelly3.File2, (byte)0xFF);

    private byte GetPass1()
        => ReadValue(InGameScenarioOffsets.Pass1.File1, default, (byte)0xFF);

    private byte GetPass2()
        => ReadValue(InGameScenarioOffsets.Pass2.File1, default, (byte)0xFF);

    private byte GetPass3()
        => ReadValue(InGameScenarioOffsets.Pass3.File1, default, (byte)0xFF);

    private short GetPass4()
        => ReadValue(InGameScenarioOffsets.Pass4.File1, InGameScenarioOffsets.Pass4.File2, (short)-1);

    private byte GetPass5()
        => ReadValue(InGameScenarioOffsets.Pass5.File1, default, (byte)0xFF);

    private byte GetPass6()
        => ReadValue(InGameScenarioOffsets.Pass6.File1, default, (byte)0xFF);

    private byte GetDifficulty()
        => ReadValue(InGameScenarioOffsets.Difficulty.File1, InGameScenarioOffsets.Difficulty.File2, (byte)0xFF);

    private DecodedItem[] GetItems()
    {
        DecodedItem[] items = new DecodedItem[GameConstants.MaxItems - 1];

        nint pickupSpaceStart = GetFileSpecificOffsets(InGameScenarioOffsets.PickupSpaceStart)[0];
        int pickupStructSize = GetFileSpecificSingleIntOffset(InGameScenarioOffsets.PickupStructSize);

        if (pickupSpaceStart == nint.Zero || pickupStructSize is 0)
        {
            Logger.LogWarning("Failed to obtain valid pickup space start address or struct size. Returning empty items array.");
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

            items[i] = new DecodedItem
            {
                Id = (short)(i + 1),
                RoomId = ReadValue<byte>(pickupSpaceStart, [itemBaseOffset + roomIdOffset]),
                SlotIndex = ReadValue<byte>(pickupSpaceStart, [itemBaseOffset + slotIndexOffset]),
                TypeName = GetItemTypeName(ReadValue<short>(pickupSpaceStart, [itemBaseOffset + typeIdOffset])),
                Mix = ReadValue<byte>(pickupSpaceStart, [itemBaseOffset + mixOffset]),
                Present = ReadValue<int>(pickupSpaceStart, [itemBaseOffset + presentOffset]),
                Quantity = ReadValue<short>(pickupSpaceStart, [itemBaseOffset + quantityOffset]),
                PickedUp = ReadValue<short>(pickupSpaceStart, [itemBaseOffset + pickedUpOffset])
            };
        }

        return items;
    }

    public bool IsInScenario()
        => GetFrameCount() > 0 && GetScenarioStatus() > 0;

    public void UpdateScenario(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Logger.LogDebug("Decoding scenario");

        DecodedScenario = new DecodedInGameScenario
        {
            CurrentFile = (byte)CurrentFile,
            ScenarioName = GetScenarioName(GetScenarioId()),
            FrameCounter = GetFrameCount(),
            Status = GetScenarioStatus(),
            PlayerCount = GetPlayerCount(),
            WildThingsTime = GetWildThingsTime(),
            EscapeTime = GetEscapeTime(),
            FightTime = GetDesperateTimesFightTime(),
            FightTime2 = GetDesperateTimesFightTime2(),
            GarageTime = GetDesperateTimesGarageTime(),
            GasTime = GetDesperateTimesGasTime(),
            GasFlag = GetDesperateTimesGasFlag(),
            GasRandom = GetDesperateTimesGasRandom(),
            ItemRandom = GetItemRandom(),
            ItemRandom2 = GetItemRandom2(),
            PuzzleRandom = GetPuzzleRandom(),
            Coin = GetCoin(),
            KilledZombie = GetKilledZombies(),
            PassWildThings = GetPassWildThings(),
            PassDesperateTimes1 = GetPassDesperateTimes(),
            PassDesperateTimes2 = GetPassDesperateTimes2(),
            PassDesperateTimes3 = GetPassDesperateTimes3(),
            Pass1 = CurrentFile == GameFile.FileOne ? GetPass1() : (byte)0,
            Pass2 = CurrentFile == GameFile.FileOne ? GetPass2() : (byte)0,
            Pass3 = CurrentFile == GameFile.FileOne ? GetPass3() : (byte)0,
            PassUnderbelly1 = GetPassUnderBelly1(),
            PassUnderbelly2 = GetPassUnderBelly2(),
            PassUnderbelly3 = GetPassUnderBelly3(),
            Pass4 = GetPass4(),
            Pass5 = CurrentFile == GameFile.FileOne ? GetPass5() : (byte)0,
            Pass6 = CurrentFile == GameFile.FileOne ? GetPass6() : (byte)0,
            Difficulty = GetDifficultyName(GetDifficulty()),
            Items = GetItems()
        };

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded scenario in {Duration}ms", duration);
        string jsonObject = JsonSerializer.Serialize(DecodedScenario, DecodedScenarioJsonContext.Default.DecodedInGameScenario);
        Logger.LogDebug("Decoded scenario: {JsonObject}", jsonObject);
    }
}