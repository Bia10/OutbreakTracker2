using FastEnumUtility;
using OutbreakTracker2.Memory;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Extensions;
using OutbreakTracker2.PCSX2;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OutbreakTracker2.Outbreak.Readers;

public abstract class ReaderBase
{
    protected readonly GameClient GameClient;
    protected readonly EEmemMemory EEmemMemory;
    protected readonly IMemoryReader MemoryReader;
    protected readonly GameFile CurrentFile;

    protected ReaderBase(GameClient gameClient, EEmemMemory eememMemory)
    {
        GameClient = gameClient;
        EEmemMemory = eememMemory;
        MemoryReader = eememMemory.MemoryReader;

        CurrentFile = GetGameFile();
    }

    private T Read<T>(nint address) where T : struct =>
        MemoryReader.Read<T>(GameClient.Handle, address);

    private string ReadString(nint address, Encoding? encoding = null) =>
        MemoryReader.ReadString(GameClient.Handle, address, encoding);

    private GameFile GetGameFile()
    {
        byte f1Byte = Read<byte>(EEmemMemory.GetAddressFromPtr(FileOnePtrs.DiscStart));
        byte f2Byte = Read<byte>(EEmemMemory.GetAddressFromPtr(FileTwoPtrs.DiscStart));

        if (f1Byte == 0x53) return GameFile.FileOne;

        return f2Byte == 0x53 ? GameFile.FileTwo : GameFile.Unknown;
    }

    protected T ReadValue<T>((nint[] File1, nint[] File2) offsets, string methodName, T errorValue) where T : struct
        => ReadValueFromOffsets(offsets.File1, offsets.File2, methodName, errorValue);

    protected T ReadSlotValue<T>(int slotIndex, (nint[] File1, nint[] File2) offsets, string methodName, T errorValue) where T : struct
        => ReadSlotValue(slotIndex, offsets.File1, offsets.File2, methodName, errorValue);

    protected string ReadSlotString(int slotIndex, (nint[] File1, nint[] File2) offsets, string methodName, string errorValue)
        => ReadSlotValue(slotIndex, offsets.File1, offsets.File2, methodName, errorValue);

    protected T ReadSlotValue<T>(int slotIndex, nint[] offsetsFile1, nint[] offsetsFile2, string methodName, T errorValue)
        where T : struct
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

    protected T ReadValue<T>(nint address, string methodName) where T : struct
    {
        T result = Read<T>(address);
        Console.WriteLine($"[{methodName}] Successfully read type {typeof(T)} at 0x{address:X} obtained value {result}.");
        return result;
    }

    protected string ReadSlotValue(int slotIndex, nint[] offsetsFile1, nint[] offsetsFile2, string methodName, string errorValue)
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

    protected static string GetEnumString<TEnum>(object value, TEnum defaultValue)
        where TEnum : struct, Enum
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

    protected string GetScenarioString<TFileOne, TFileTwo>(short scenarioId, TFileOne defaultFileOne, TFileTwo defaultFileTwo) 
        where TFileOne : struct, Enum
        where TFileTwo : struct, Enum
    {
        return CurrentFile switch
        {
            GameFile.FileOne => GetEnumString(scenarioId, defaultFileOne),
            GameFile.FileTwo => GetEnumString(scenarioId, defaultFileTwo),
            _ => throw new InvalidOperationException($"[{nameof(GetScenarioString)}] Unrecognized game file: {CurrentFile}")
        };
    }

    private bool TryComputeAddress(
        int slotIndex, 
        nint[] offsetsFile1, 
        nint[] offsetsFile2, 
        string methodName,
        out nint address,
        [NotNullWhen(false)] out string? errorMessage)
    {
        address = nint.Zero;
        errorMessage = null;

        if (!slotIndex.IsSlotIndexValid())
        {
            errorMessage = $"[{methodName}] Invalid slot index: {slotIndex}. Valid range is 0 to {Constants.MaxLobbySlots - 1}.";
            return false;
        }

        nint[] offsets = GetOffsets(offsetsFile1, offsetsFile2);
        nint basePtr = GetBasePointer(slotIndex);

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

    protected bool TryComputeAddress(
        nint[] offsetsFile1,
        nint[] offsetsFile2,
        string methodName,
        out nint address,
        [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;

        nint[] offsets = GetOffsets(offsetsFile1, offsetsFile2);
        address = ComputeAddress(offsets);

        if (address.IsValidAddress())
            return true;

        errorMessage = $"[{methodName}] Invalid address: {address:X}";
        return false;
    }

    protected T ReadValueFromOffsets<T>(nint[] offsetsFile1, nint[] offsetsFile2, string methodName, T errorValue)
        where T : struct
    {
        if (!TryComputeAddress(offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Console.WriteLine(errorMessage);
            return errorValue;
        }

        T result = Read<T>(address);
        Console.WriteLine($"[{methodName}] Successfully read type {typeof(T)} at 0x{address:X} obtained value {result}.");
        return result;
    }

    protected string ReadStringFromOffsets(nint[] offsetsFile1, nint[] offsetsFile2, string methodName, string errorValue)
    {
        if (!TryComputeAddress(offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Console.WriteLine(errorMessage);
            return errorValue;
        }

        string result = ReadString(address);
        Console.WriteLine($"[{methodName}] Successfully read string at 0x{address:X} obtained value {result}.");
        return result;
    }

    protected nint[] GetOffsets(nint[] offsetsFile1, nint[] offsetsFile2)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => offsetsFile1,
            GameFile.FileTwo => offsetsFile2,
            _ => []
        };
    }

    private nint GetBasePointer(int slotIndex)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyAddress(slotIndex),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyAddress(slotIndex),
            _ => nint.Zero
        };
    }

    protected nint ComputeAddress(nint basePtr, nint[] offsets)
        => offsets.Length == 0
            ? EEmemMemory.GetAddressFromPtr(basePtr)
            : EEmemMemory.GetAddressFromPtrChain(basePtr, offsets);

    protected nint ComputeAddress(nint[] offsets)
        => offsets.Length == 1
            ? EEmemMemory.GetAddressFromPtr(offsets.First())
            : EEmemMemory.GetAddressFromPtrChain(offsets.First(), offsets);
}
