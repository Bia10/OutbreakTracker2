using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class DoorReader : ReaderBase
{
    public DecodedDoor[] DecodedDoors { get; private set; }

    public DoorReader(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedDoors = new DecodedDoor[GameConstants.MaxDoors];
        for (int i = 0; i < GameConstants.MaxDoors; i++)
            DecodedDoors[i] = new DecodedDoor();
    }

    public ushort GetHealthPoints(int doorId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetDoorHealthAddress(doorId)),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetDoorHealthAddress(doorId)),
        _ => 0xFF
    };

    public ushort GetFlag(int doorId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetDoorFlagAddress(doorId)),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetDoorFlagAddress(doorId)),
        _ => 0xFF
    };

    public static string DecodeFlag(ushort doorHp, ushort flag)
    {
        if (doorHp is 500)
            return "unlocked";

        return flag switch
        {
            0 => "unlocked",
            1 => "locked",
            2 => "locked", //Fragile lock wild things
            3 => "locked", //Fragile lock underbelly, elephant restaurant(simple lock)
            4 => "locked", //Fragile lock underbelly
            6 => "unknownState6",
            8 => "locked", //Fragile lock wild things
            10 => "unlocked",
            12 => "unlocked",
            13 => "unknownState13", // Wild things
            18 => "unknownState14", // Flashback
            44 => "unlocked",
            130 => "unlocked",
            2000 => "unlocked",
            _ => flag.ToString()
        };
    }

    public void UpdateDoors(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Logger.LogDebug("Decoding doors");

        int maxDoors = CurrentFile switch
        {
            GameFile.FileOne => GameConstants.MaxDoors - 9,
            GameFile.FileTwo => GameConstants.MaxDoors,
            _ => 0
        };

        DecodedDoor[] newDecodedDoors = new DecodedDoor[GameConstants.MaxDoors];

        for (int i = 0; i < maxDoors; i++)
        {
            Ulid doorUlid = GetPersistentUlidForDoorSlot(i);

            newDecodedDoors[i] = new DecodedDoor
            {
                Id = doorUlid,
                Hp = GetHealthPoints(i),
                Flag = GetFlag(i),
                Status = DecodeFlag(GetHealthPoints(i), GetFlag(i))
            };
        }

        DecodedDoors = newDecodedDoors;

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded doors in {Duration}ms", duration);
        foreach (string jsonObject in DecodedDoors.Select(door
                     => JsonSerializer.Serialize(door, DecodedDoorJsonContext.Default.DecodedDoor)))
            Logger.LogDebug("Decoded door: {JsonObject}", jsonObject);
    }

    private readonly Dictionary<int, Ulid> _doorSlotUlids = new();

    private Ulid GetPersistentUlidForDoorSlot(int doorSlotIndex)
    {
        if (_doorSlotUlids.TryGetValue(doorSlotIndex, out Ulid ulid))
            return ulid;

        ulid = Ulid.NewUlid();
        _doorSlotUlids.Add(doorSlotIndex, ulid);

        return ulid;
    }
}