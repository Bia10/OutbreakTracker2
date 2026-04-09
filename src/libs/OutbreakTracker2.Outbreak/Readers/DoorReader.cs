using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class DoorReader : ReaderBase, IDoorReader
{
    public DecodedDoor[] DecodedDoors { get; private set; }

    private enum DoorPropertyType
    {
        Health,
        Flag,
    }

    private const ushort DefaultHealthErrorValue = 0xFF;
    private const ushort DefaultFlagErrorValue = 0xFF;

    private readonly IReadOnlyDictionary<GameFile, IDoorAddressProvider> _addressProviders;

    public DoorReader(
        IGameClient gameClient,
        IEEmemAddressReader eememMemory,
        ILogger logger,
        IEnumerable<IDoorAddressProvider> addressProviders
    )
        : base(gameClient, eememMemory, logger)
    {
        _addressProviders = addressProviders.ToDictionary(p => p.SupportedFile);

        DecodedDoors = new DecodedDoor[GameConstants.MaxDoors];
        for (int i = 0; i < GameConstants.MaxDoors; i++)
            DecodedDoors[i] = new DecodedDoor();
    }

    private T ReadDoorProperty<T>(int doorId, DoorPropertyType propertyType, T errorValue)
        where T : unmanaged
    {
        if (!_addressProviders.TryGetValue(CurrentFile, out IDoorAddressProvider? provider))
            return errorValue;

        nint doorPropertyAddress = propertyType switch
        {
            DoorPropertyType.Health => provider.GetHealthAddress(doorId),
            DoorPropertyType.Flag => provider.GetFlagAddress(doorId),
            _ => nint.Zero,
        };

        return doorPropertyAddress <= nint.Zero ? errorValue : ReadValue<T>(doorPropertyAddress);
    }

    private ushort GetHealthPoints(int doorId) =>
        ReadDoorProperty(doorId, DoorPropertyType.Health, DefaultHealthErrorValue);

    private ushort GetFlag(int doorId) => ReadDoorProperty(doorId, DoorPropertyType.Flag, DefaultFlagErrorValue);

    private string DecodeFlag(ushort doorHp, ushort flag)
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
            18 => "unknownState18", // Flashback
            44 => "unlocked",
            130 => "unlocked",
            2000 => "unlocked",
            _ => LogUnknownFlag(flag),
        };
    }

    private string LogUnknownFlag(ushort flag)
    {
        Logger.LogTrace("Unknown door flag value {Value}", flag);
        return flag.ToString();
    }

    public void UpdateDoors()
    {
        if (CurrentFile is GameFile.Unknown)
            return;

        if (!_addressProviders.TryGetValue(CurrentFile, out IDoorAddressProvider? provider))
            return;

        DecodedDoor[] newDecodedDoors = new DecodedDoor[GameConstants.MaxDoors];

        for (int i = 0; i < provider.MaxDoors; i++)
        {
            Ulid doorUlid = GetPersistentUlidForDoorSlot(i);
            ushort curHp = GetHealthPoints(i);
            ushort flag = GetFlag(i);

            newDecodedDoors[i] = new DecodedDoor
            {
                Id = doorUlid,
                Hp = curHp,
                Flag = flag,
                Status = DecodeFlag(curHp, flag),
            };
        }

        DecodedDoors = newDecodedDoors;
    }

    private readonly ConcurrentDictionary<int, Ulid> _doorSlotUlids = [];

    private Ulid GetPersistentUlidForDoorSlot(int doorSlotIndex) =>
        _doorSlotUlids.GetOrAdd(doorSlotIndex, static _ => Ulid.NewUlid());
}
