using System.Diagnostics.CodeAnalysis;
using FastEnumUtility;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2Memory;
using System.Text.Json;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Extensions;

namespace OutbreakTracker2.Outbreak.Readers;

public class LobbyReader : ReaderBase
{
    public LobbyReader(GameClient gameClient, EEmemMemory memory) : base(gameClient, memory) { }

    private bool TryComputeAddress(int slotIndex, nint[] offsetsFile1, nint[] offsetsFile2, string methodName, out nint address, [NotNullWhen(false)] out string? errorMessage)
    {
        address = nint.Zero;
        errorMessage = null;

        if (!slotIndex.IsSlotIndexValid())
        {
            errorMessage = $"[{methodName}] Invalid slot index: {slotIndex}. Valid range is 0 to {Constants.MaxLobbySlots - 1}.";
            return false;
        }

        nint[] offsets = GetOffsets(CurrentFile, offsetsFile1, offsetsFile2);
        nint basePtr = GetBasePointer(CurrentFile, slotIndex);

        if (!basePtr.IsValidAddress())
        {
            errorMessage = $"[{methodName}] Invalid base pointer for slot index {slotIndex}.";
            return false;
        }

        address = ComputeAddress(basePtr, offsets);

        if (address.IsValidAddress())
            return true;

        errorMessage = $"[{methodName}] Invalid address for slot index {slotIndex}: {address:X}";
        return false;
    }

    private T ReadSlotValue<T>(int slotIndex, nint[] offsetsFile1, nint[] offsetsFile2, string methodName, T errorValue) where T : struct
    {
        if (!TryComputeAddress(slotIndex, offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Console.WriteLine(errorMessage);
            return errorValue;
        }

        T result = Read<T>(address);
        Console.WriteLine($"[{methodName}] Successfully read type {typeof(T)} at 0x{address:X} obtained value {result}.");
        return result;
    }

    private string ReadSlotValue(int slotIndex, nint[] offsetsFile1, nint[] offsetsFile2, string methodName, string errorValue)
    {
        if (!TryComputeAddress(slotIndex, offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Console.WriteLine(errorMessage);
            return errorValue;
        }

        string result = ReadString(address);
        Console.WriteLine($"[{methodName}] Successfully read string at 0x{address:X} obtained value {result}.");
        return result;
    }

    private static nint[] GetOffsets(GameFile currentFile, nint[] offsetsFile1, nint[] offsetsFile2)
    {
        return GameFile switch
        {
            GameFile.FileOne => offsetsFile1,
            GameFile.FileTwo => offsetsFile2,
            _ => []
        };
    }

    private static nint GetBasePointer(GameFile currentFile, int slotIndex)
    {
        return GameFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyAddress(slotIndex),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyAddress(slotIndex),
            _ => nint.Zero
        };
    }

    private nint ComputeAddress(nint basePtr, nint[] offsets)
    {
        return offsets.Length == 0
            ? EEmemMemory.GetAddressFromPtr(basePtr)
            : EEmemMemory.GetAddressFromPtrChain(basePtr, offsets);
    }

    public short GetIndex(int slotIndex) => ReadSlotValue(slotIndex, [], [], nameof(GetIndex), (short)-1);
    public short GetCurPlayer(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotPlayer], [FileTwoPtrs.LobbySlotPlayer], nameof(GetCurPlayer), (short)-1);
    public short GetMaxPlayer(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotMaxPlayer], [FileTwoPtrs.LobbySlotMaxPlayer], nameof(GetMaxPlayer), (short)-1);
    public byte GetStatus(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotStatus], [FileTwoPtrs.LobbySlotStatus], nameof(GetStatus), (byte)SlotStatus.Unknown);
    public byte GetPass(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotPass], [FileTwoPtrs.LobbySlotPass], nameof(GetPass), (byte)SlotPass.NoPass);
    public short GetScenarioId(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotScenarioID], [FileTwoPtrs.LobbySlotScenarioID], nameof(GetScenarioId), (short)FileTwoScenario.Unknown);
    public short GetVersion(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotVersion], [FileTwoPtrs.LobbySlotVersion], nameof(GetVersion), (short)GameVersion.Unknown);
    public string GetTitle(int slotIndex) => ReadSlotValue(slotIndex, [FileOnePtrs.LobbySlotTitle], [FileTwoPtrs.LobbySlotTitle], nameof(GetTitle), string.Empty);

    private static string GetEnumString<TEnum>(object value, TEnum defaultValue) where TEnum : struct, Enum
    {
        if (FastEnum.TryParse(value.ToString(), out TEnum result))
        {
            string? enumValue = result.GetEnumMemberValue();
            if (!string.IsNullOrEmpty(enumValue))
                return enumValue;

            Console.WriteLine($"[{nameof(GetEnumString)}] Enum value resolved to null or empty for type {typeof(TEnum).Name} and value {value}.");
            throw new InvalidOperationException($"Enum value cannot be null or empty for type {typeof(TEnum).Name}.");
        }

        Console.WriteLine($"[{nameof(GetEnumString)}] Failed to parse enum for type {typeof(TEnum).Name} and value {value}. Defaulting to {defaultValue}.");
        return defaultValue.ToString();
    }

    public string GetStatusString(int slotIndex) => GetEnumString(GetStatus(slotIndex), Enums.SlotStatus.Unknown);
    public string GetPassString(int slotIndex) => GetEnumString(GetPass(slotIndex), Enums.SlotPass.NoPass);
    public string GetVersionString(int slotIndex) => GetEnumString(GetVersion(slotIndex), Enums.GameVersion.Unknown);

    public string GetScenarioString(int slotIndex)
    {
        short scenarioId = GetScenarioId(slotIndex);
        return CurrentFile switch
        {
            GameFile.FileOne => GetEnumString(scenarioId, Enums.FileOneScenario.Unknown),
            GameFile.FileTwo => GetEnumString(scenarioId, Enums.FileTwoScenario.Unknown),
            _ => throw new InvalidOperationException($"[{nameof(GetScenarioString)}] Unable to recognize current game file {CurrentFile.ToString()}")
        };
    }

    public DecodedLobbySlot[] DecodedLobbySlots { get; } = new DecodedLobbySlot[Constants.MaxLobbySlots];

    public void UpdateLobbies(bool debug = false)
    {
        if (CurrentFile == GameFile.Unknown) return;

        for (var i = 0; i < Constants.MaxLobbySlots; i++)
        {
            Console.WriteLine($"Decoding lobby at slot index: {i}");

            DecodedLobbySlots[i] = new DecodedLobbySlot
            {
                SlotNumber = GetIndex(i),
                CurPlayers = GetCurPlayer(i),
                MaxPlayers = GetMaxPlayer(i),
                Status = GetStatusString(i),
                IsPassProtected = GetPassString(i),
                ScenarioId = GetScenarioString(i),
                Version = GetVersionString(i),
                Title = GetTitle(i)
            };
        }

        if (!debug) return;

        foreach (DecodedLobbySlot lobbySlot in DecodedLobbySlots)
            Console.WriteLine(JsonSerializer.Serialize(lobbySlot, DecodedLobbySlotJsonContext.Default.DecodedLobbySlot));
    }
}