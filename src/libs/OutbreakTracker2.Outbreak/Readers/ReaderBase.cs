using Microsoft.Extensions.Logging;
using OutbreakTracker2.Memory;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Extensions;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using OutbreakTracker2.Extensions;

namespace OutbreakTracker2.Outbreak.Readers;

public abstract class ReaderBase
{
    private readonly GameClient _gameClient;
    private readonly IEEmemMemory _eEmemMemory;
    private readonly IMemoryReader _memoryReader;
    protected readonly ILogger Logger;
    private const bool EnableLogging = false;
    protected GameFile CurrentFile { get; }

    protected ReaderBase(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
    {
        _gameClient = gameClient;
        _eEmemMemory = eememMemory;
        _memoryReader = eememMemory.MemoryReader;
        Logger = logger;

        CurrentFile = GetGameFile();
    }

    private T Read<T>(nint address) where T : struct
        => _memoryReader.Read<T>(_gameClient.Handle, address);

    private string ReadString(nint address, Encoding? encoding = null)
        => _memoryReader.ReadString(_gameClient.Handle, address, encoding);

    private GameFile GetGameFile()
    {
        byte f1Byte = Read<byte>(_eEmemMemory.GetAddressFromPtr(FileOnePtrs.DiscStart));
        byte f2Byte = Read<byte>(_eEmemMemory.GetAddressFromPtr(FileTwoPtrs.DiscStart));

        if (f1Byte is 0x53)
        {
            Logger.LogDebug("Detected GameFile.FileOne");
            return GameFile.FileOne;
        }

        if (f2Byte is 0x53)
        {
            Logger.LogDebug("Detected GameFile.FileTwo");
            return GameFile.FileTwo;
        }

        Logger.LogWarning(
            "Failed to detect GameFile. f1Byte: {F1Byte}, f2Byte: {F2Byte}",
            f1Byte, f2Byte
        );

        return GameFile.Unknown;
    }

    protected T ReadValue<T>(
        (nint[] File1, nint[] File2) offsets,
        T errorValue,
        [CallerMemberName] string methodName = ""
    ) where T : struct
        => ReadValueFromOffsets(offsets.File1, offsets.File2, methodName, errorValue);

    protected T ReadSlotValue<T>(
        int slotIndex,
        (nint[] File1, nint[] File2) offsets,
        T errorValue,
        [CallerMemberName] string methodName = ""
    ) where T : struct
        => ReadSlotValue(slotIndex, offsets.File1, offsets.File2, errorValue, methodName);

    protected string ReadSlotString(
        int slotIndex,
        (nint[] File1, nint[] File2) offsets,
        string errorValue,
        [CallerMemberName] string methodName = ""
    ) => ReadSlotValue(slotIndex, offsets.File1, offsets.File2, errorValue, methodName);

    protected T ReadSlotValue<T>(
        int slotIndex,
        nint[] offsetsFile1,
        nint[] offsetsFile2,
        T errorValue,
        [CallerMemberName] string methodName = ""
    ) where T : struct
    {
        if (!TryComputeLobbyAddress(slotIndex, offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Logger.LogError(errorMessage);
            return errorValue;
        }

        T result = Read<T>(address);
        if (EnableLogging)
            Logger.LogTrace("[{MethodName}] Successfully read type {Type} at 0x{Address:X} obtained value {Result}", methodName, typeof(T), address, result);

        return result;
    }

    protected T ReadValue<T>(
        nint basePtr,
        ReadOnlySpan<nint> offsets = default,
        [CallerMemberName] string methodName = ""
    ) where T : struct
    {
        nint address = ComputeAddress(basePtr, offsets);
        T result = Read<T>(address);

        if (EnableLogging)
            Logger.LogTrace("[{MethodName}] Read {Type} at 0x{Address:X}: {Result}", methodName, typeof(T), address, result);

        return result;
    }


    protected string ReadSlotValue(
        int slotIndex,
        nint[] offsetsFile1,
        nint[] offsetsFile2,
        string errorValue,
        [CallerMemberName] string methodName = ""
    )
    {
        if (!TryComputeLobbyAddress(slotIndex, offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Logger.LogError(errorMessage);
            return errorValue;
        }

        string result = ReadString(address);

        if (EnableLogging)
            Logger.LogTrace("[{MethodName}] Successfully read string at 0x{Address:X} obtained value {Result}", methodName, address, result);

        return result;
    }

    protected bool TryComputeLobbyAddress(
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
            errorMessage = $"[{methodName}] Invalid slot index: {slotIndex}. Valid range is 0 to {GameConstants.MaxLobbySlots - 1}.";
            return false;
        }

        nint[] offsets = GetOffsets(offsetsFile1, offsetsFile2);
        nint basePtr = GetLobbyBasePointer(slotIndex);

        if (!basePtr.IsNeitherNullNorNegative())
        {
            errorMessage = $"[{methodName}] Invalid base pointer for slot index {slotIndex}.";
            return false;
        }

        address = ComputeAddress(basePtr, offsets);

        if (address.IsNeitherNullNorNegative())
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

        if (address.IsNeitherNullNorNegative())
            return true;

        errorMessage = $"[{methodName}] Invalid address: {address:X}";
        return false;
    }

    protected T ReadValueFromOffsets<T>(nint[] offsetsFile1, nint[] offsetsFile2, string methodName, T errorValue)
        where T : struct
    {
        if (!TryComputeAddress(offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Logger.LogError(errorMessage);
            return errorValue;
        }

        T result = Read<T>(address);
        if (EnableLogging)
            Logger.LogTrace("[{MethodName}] Successfully read type {Type} at 0x{Address:X} obtained value {Result}", methodName, typeof(T), address, result);

        return result;
    }

    protected string ReadStringFromOffsets(nint[] offsetsFile1, nint[] offsetsFile2, string methodName, string errorValue)
    {
        if (!TryComputeAddress(offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Logger.LogError(errorMessage);
            return errorValue;
        }

        string result = ReadString(address);
        if (EnableLogging)
            Logger.LogTrace("[{MethodName}] Successfully read string at 0x{Address:X} obtained value {Result}", methodName, address, result);

        return result;
    }

    protected string GetScenarioString<TFileOne, TFileTwo>(short scenarioId, TFileOne defaultFileOne, TFileTwo defaultFileTwo)
        where TFileOne : struct, Enum
        where TFileTwo : struct, Enum
    {
        return CurrentFile switch
        {
            GameFile.FileOne => EnumUtility.GetEnumString(scenarioId, defaultFileOne),
            GameFile.FileTwo => EnumUtility.GetEnumString(scenarioId, defaultFileTwo),
            _ => throw new InvalidOperationException($"[{nameof(GetScenarioString)}] Unrecognized game file: {CurrentFile}")
        };
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

    protected nint GetLobbyBasePointer(int slotIndex)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyAddress(slotIndex),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyAddress(slotIndex),
            _ => nint.Zero
        };
    }

    protected nint GetLobbyRoomPlayerBasePointer(int characterId)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyRoomPlayerAddress(characterId),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyRoomPlayerAddress(characterId),
            _ => nint.Zero
        };
    }

    protected nint ComputeAddress(nint basePtr, params ReadOnlySpan<nint> offsets)
        => offsets.Length is 0
            ? _eEmemMemory.GetAddressFromPtr(basePtr)
            : _eEmemMemory.GetAddressFromPtrChain(basePtr, offsets);

    protected nint ComputeAddress(params ReadOnlySpan<nint> offsets)
        => offsets.Length is 1
            ? _eEmemMemory.GetAddressFromPtr(offsets[0])
            : _eEmemMemory.GetAddressFromPtrChain(offsets[0], offsets);
}
