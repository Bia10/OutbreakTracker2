using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public class DoorReader : ReaderBase
{
    public DoorReader(GameClient gameClient, EEmemMemory eememMemory) : base(gameClient, eememMemory)
    {
    }

    public ushort GetHealthPoints(int doorID) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetDoorHealthAddress(doorID), nameof(GetHealthPoints)),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetDoorHealthAddress(doorID), nameof(GetHealthPoints)),
        _ => 0xFF
    };

    public ushort GetFlag(int doorID) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetDoorFlagAddress(doorID), nameof(GetFlag)),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetDoorFlagAddress(doorID), nameof(GetFlag)),
        _ => 0xFF
    };

    public string DecodeFlag(ushort doorHP, ushort flag)
    {
        if (doorHP is 500)
            return "unlocked";
            
        return flag switch
        {
            0 => "unlocked",
            1 => "locked",
            2 => "locked", //Fragile lock wild things
            3 => "locked", //Fragile lock underbelly
            4 => "locked", //Fragile lock underbelly
            6 => "unknownState",
            8 => "locked", //Fragile lock wild things
            10 => "unlocked",
            12 => "unlocked",
            13 => "unknownState",
            18 => "unknownState",
            44 => "unlocked",
            130 => "unlocked",
            2000 => "unlocked",
            _ => flag.ToString()
        };
    }

    public DecodedDoor[] DecodedDoors { get; } = new DecodedDoor[Constants.MaxDoors];

    public void UpdateDoors(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Console.WriteLine("Decoding doors");

        var maxDoors = CurrentFile switch
        {
            GameFile.FileOne => Constants.MaxDoors - 9,
            GameFile.FileTwo => Constants.MaxDoors,
            _ => 0
        };

        for (var i = 0; i < maxDoors; i++)
        {
            DecodedDoors[i].HP = GetHealthPoints(i);
            DecodedDoors[i].Flag = GetFlag(i);
            DecodedDoors[i].Status = DecodeFlag(DecodedDoors[i].HP, DecodedDoors[i].Flag);
        }

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Console.WriteLine($"Decoded doors in {duration}ms");

        foreach (DecodedDoor door in DecodedDoors)
            Console.WriteLine(JsonSerializer.Serialize(door, DecodedDoorJonContext.Default.DecodedDoor));
    }
}
