using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public class InGameScenarioReader : ReaderBase
{
    public DecodedScenario DecodedScenario { get; }

    public InGameScenarioReader(GameClient gameClient, EEmemMemory memory, ILogger logger)
        : base(gameClient, memory, logger)
    {
        DecodedScenario = new DecodedScenario();
    }

    public static string GetScenarioName(short scenarioId)
        => EnumUtility.GetEnumString(scenarioId, InGameScenario.Unknown);

    public static string GetDifficultyName(byte difficulty)
        => EnumUtility.GetEnumString(difficulty, RoomDifficulty.Unknown);
    
    public short GetScenarioId() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.InGameScenarioId),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.InGameScenarioId),
        _ => -1
    };

    public int GetFrameCount() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<int>(FileOnePtrs.InGameFrameCounter),
        GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.InGameFrameCounter),
        _ => -1
    };

    public byte GetIsScenarioCleared() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.IsScenarioCleared),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.IsScenarioCleared),
        _ => 0xFF
    };

    public byte GetPlayerCount() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.InGamePlayerNumber),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.IngamePlayerNumber),
        _ => 0xFF
    };

    public short GetWildThingsTime() => ReadValue<short>(FileTwoPtrs.WildThingsTime);

    public short GetEscapeTime() => ReadValue<short>(FileTwoPtrs.EscapeTime);

    public int GetDesperateTimesFightTime() => ReadValue<int>(FileTwoPtrs.DesperateTimesFightTime);

    public short GetDesperateTimesFightTime2() => ReadValue<short>(FileTwoPtrs.DesperateTimesFightTime2);

    public int GetDesperateTimesGarageTime() => ReadValue<short>(FileTwoPtrs.DesperateTimesGarageTime);

    public int GetDesperateTimesGasTime() => ReadValue<short>(FileTwoPtrs.DesperateTimesGasTime);

    public int GetDesperateTimesGasFlag() => ReadValue<short>(FileTwoPtrs.DesperateTimesGasFlag);

    public byte GetDesperateTimesGasRandom() => ReadValue<byte>(FileTwoPtrs.DesperateTimesGasRandom);

    public byte GetItemRandom() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileTwoPtrs.ItemRandom),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.ItemRandom),
        _ => 0xFF
    };

    public byte GetItemRandom2() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileTwoPtrs.ItemRandom2),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.ItemRandom2),
        _ => 0xFF
    };

    public byte GetPuzzleRandom() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileTwoPtrs.PuzzleRandom),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.PuzzleRandom),
        _ => 0xFF
    };

    public byte GetCoin()
    {
        byte coin1 = ReadValue<byte>(FileTwoPtrs.Coin);
        byte coin2 = ReadValue<byte>(FileTwoPtrs.Coin, [0x2]);
        byte coin3 = ReadValue<byte>(FileTwoPtrs.Coin, [0x4]);
        byte coin4 = ReadValue<byte>(FileTwoPtrs.Coin, [0x6]);

        return (byte)(coin1 + coin2 + coin3 + coin4);
    }

    public byte GetKilledZombies()
        => ReadValue<byte>(FileTwoPtrs.KilledZombie);

    public byte GetPassWildThings()
        => ReadValue<byte>(FileTwoPtrs.PassWildThings);

    public short GetPassDesperateTimes()
        => ReadValue<short>(FileTwoPtrs.PassDesperateTimes);

    public byte GetPassDesperateTimes2()
        => ReadValue<byte>(FileTwoPtrs.PassDesperateTimes2);

    public byte GetPassDesperateTimes3()
        => ReadValue<byte>(FileTwoPtrs.PassDesperateTimes3);

    public short GetPassUnderBelly1()
        => ReadValue<byte>(FileTwoPtrs.PassUnderBelly1);

    public byte GetPassUnderBelly2()
        => ReadValue<byte>(FileTwoPtrs.PassUnderBelly2);

    public byte GetPassUnderBelly3()
        => ReadValue<byte>(FileTwoPtrs.PassUnderBelly3);

    public byte GetPass1()
        => ReadValue<byte>(FileOnePtrs.Pass1);

    public byte GetPass2()
        => ReadValue<byte>(FileOnePtrs.Pass2);

    public byte GetPass3()
        => ReadValue<byte>(FileOnePtrs.Pass3);

    public short GetPass4() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileTwoPtrs.Pass4),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.Pass4),
        _ => 0xFF
    };

    public byte GetPass5()
        => ReadValue<byte>(FileOnePtrs.Pass5);

    public byte GetPass6()
        => ReadValue<byte>(FileOnePtrs.Pass6);

    public byte GetDifficulty() => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.Difficulty),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.Difficulty),
        _ => 0xFF
    };

    public DecodedItem[] GetItems()
    {
        var Items = new DecodedItem[GameConstants.MaxItems - 1];
        byte roomId = 0;
        byte number = 0;
        short typeId = 0;
        byte mix = 0;
        int present = 0;
        short pickupCount = 0;
        short pickup = 0;

        for (int i = 0; i < GameConstants.MaxItems - 1; i++)
         {
             if (CurrentFile == GameFile.FileOne)
             {
                 nint itemBaseOffset = FileOnePtrs.PickupStructSize * i;
                 roomId = ReadValue<byte>(FileOnePtrs.PickupSpaceStart, [itemBaseOffset + FileOnePtrs.RoomIdOffset]);
                 number = ReadValue<byte>(FileOnePtrs.PickupSpaceStart, [itemBaseOffset + FileOnePtrs.NumberOffset]);
                 typeId  = ReadValue<short>(FileOnePtrs.PickupSpaceStart, [itemBaseOffset + FileOnePtrs.IdOffset]);
                 mix = ReadValue<byte>(FileOnePtrs.PickupSpaceStart, [itemBaseOffset + FileOnePtrs.MixOffset]);
                 present = ReadValue<int>(FileOnePtrs.PickupSpaceStart, [itemBaseOffset + FileOnePtrs.PresentOffset]);
                 pickupCount = ReadValue<short>(FileOnePtrs.PickupSpaceStart, [itemBaseOffset + FileOnePtrs.PickupCountOffset]);
                 pickup = ReadValue<short>(FileOnePtrs.PickupSpaceStart, [itemBaseOffset + FileOnePtrs.PickupOffset]);
             }
             else if (CurrentFile == GameFile.FileTwo)
             {
                 nint itemBaseOffset = FileTwoPtrs.PickupStructSize * i;
                 roomId = ReadValue<byte>(FileTwoPtrs.PickupSpaceStart, [itemBaseOffset + FileTwoPtrs.RoomIdOffset]);
                 number = ReadValue<byte>(FileTwoPtrs.PickupSpaceStart, [itemBaseOffset + FileTwoPtrs.NumberOffset]);
                 typeId  = ReadValue<short>(FileTwoPtrs.PickupSpaceStart, [itemBaseOffset + FileTwoPtrs.IdOffset]);
                 mix = ReadValue<byte>(FileTwoPtrs.PickupSpaceStart, [itemBaseOffset + FileTwoPtrs.MixOffset]);
                 present = ReadValue<int>(FileTwoPtrs.PickupSpaceStart, [itemBaseOffset + FileTwoPtrs.PresentOffset]);
                 pickupCount = ReadValue<short>(FileTwoPtrs.PickupSpaceStart, [itemBaseOffset + FileTwoPtrs.PickupCountOffset]);
                 pickup = ReadValue<short>(FileTwoPtrs.PickupSpaceStart, [itemBaseOffset + FileTwoPtrs.PickupOffset]);
             }

             Items[i] = new DecodedItem
             {
                 Id = (short)(i + 1),
                 RoomID = roomId,
                 Number = number,
                 Type = typeId,
                 Mix = mix,
                 Present = present,
                 Count = pickupCount,
                 Pick = pickup
             };
         }

        return Items;
    }

    public void UpdateScenario(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Logger.LogDebug("Decoding scenario");

        DecodedScenario.ScenarioName = GetScenarioName(GetScenarioId());
        DecodedScenario.FrameCounter = GetFrameCount();
        DecodedScenario.Cleared = GetIsScenarioCleared();
        DecodedScenario.PlayerCount = GetPlayerCount();
        DecodedScenario.WildThingsTime = GetWildThingsTime();
        DecodedScenario.EscapeTime = GetEscapeTime();
        DecodedScenario.FightTime = GetDesperateTimesFightTime();
        DecodedScenario.FightTime2 = GetDesperateTimesFightTime2();
        DecodedScenario.GarageTime = GetDesperateTimesGarageTime();
        DecodedScenario.GasTime = GetDesperateTimesGasTime();
        DecodedScenario.GasFlag = GetDesperateTimesGasFlag();
        DecodedScenario.GasRandom = GetDesperateTimesGasRandom();
        DecodedScenario.ItemRandom = GetItemRandom();
        DecodedScenario.ItemRandom2 = GetItemRandom2();
        DecodedScenario.PuzzleRandom = GetPuzzleRandom();
        DecodedScenario.Coin = GetCoin();
        DecodedScenario.KilledZombie = GetKilledZombies();
        DecodedScenario.PassWildThings = GetPassWildThings();
        DecodedScenario.PassDesperateTimes1 = GetPassDesperateTimes();
        DecodedScenario.PassDesperateTimes2 = GetPassDesperateTimes2();
        DecodedScenario.PassDesperateTimes3 = GetPassDesperateTimes3();
        DecodedScenario.Pass1 = GetPass1();
        DecodedScenario.Pass2 = GetPass2();
        DecodedScenario.Pass3 = GetPass3();
        DecodedScenario.PassUnderbelly1 = GetPassUnderBelly1();
        DecodedScenario.PassUnderbelly2 = GetPassUnderBelly2();
        DecodedScenario.PassUnderbelly3 = GetPassUnderBelly3();
        DecodedScenario.Pass4 = GetPass4();
        DecodedScenario.Pass5 = GetPass5();
        DecodedScenario.Pass6 = GetPass6();
        DecodedScenario.Difficulty = GetDifficultyName(GetDifficulty());
        DecodedScenario.Items = GetItems();

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded scenario in {Duration}ms", duration);
        string jsonObject = JsonSerializer.Serialize(DecodedScenario, DecodedScenarioJsonContext.Default.DecodedScenario);
        Logger.LogDebug("Decoded scenario: {jsonObject}", jsonObject);
    }
}
