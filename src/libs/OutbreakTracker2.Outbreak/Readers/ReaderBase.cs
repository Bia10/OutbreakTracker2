using Microsoft.Extensions.Logging;
using OutbreakTracker2.Extensions;
using OutbreakTracker2.Memory.MemoryReader;
using OutbreakTracker2.Memory.StringReader;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Extensions;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace OutbreakTracker2.Outbreak.Readers;

public abstract class ReaderBase
{
    protected readonly ILogger Logger;
    private readonly GameClient _gameClient;
    private readonly IEEmemMemory _eEmemMemory;
    private readonly ISafeMemoryReader _memoryReader;
    private readonly IStringReader _stringReader;
    protected GameFile CurrentFile { get; }

    protected ReaderBase(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
    {
        Logger = logger;
        _gameClient = gameClient;
        _eEmemMemory = eememMemory;
        _memoryReader = eememMemory.MemoryReader;
        _stringReader = eememMemory.StringReader;

        CurrentFile = GetGameFile();
    }

    private T Read<T>(nint address) where T : unmanaged
        => _memoryReader.Read<T>(_gameClient.Handle, address);

    private string ReadString(nint address, Encoding? encoding = null)
        => _stringReader.Read(_gameClient.Handle, address, encoding);

    private GameFile GetGameFile()
    {
        byte f1Byte = Read<byte>(_eEmemMemory.GetAddressFromPtr(FileOnePtrs.DiscStart));
        byte f2Byte = Read<byte>(_eEmemMemory.GetAddressFromPtr(FileTwoPtrs.DiscStart));

        if (f1Byte is 0x53)
        {
            Logger.LogInformation("Game file detected: {GameFile}. Signature byte: 0x{SignatureByte:X2}", GameFile.FileOne, f1Byte);
            return GameFile.FileOne;
        }

        if (f2Byte is 0x53)
        {
            Logger.LogInformation("Game file detected: {GameFile}. Signature byte: 0x{SignatureByte:X2}", GameFile.FileTwo, f2Byte);
            return GameFile.FileTwo;
        }

        Logger.LogWarning(
            "Failed to detect game file. FileOne signature byte: 0x{F1Byte:X2}, FileTwo signature byte: 0x{F2Byte:X2}. Defaulting to Unknown",
            f1Byte, f2Byte
        );

        return GameFile.Unknown;
    }

    protected T ReadValue<T>(
        (nint[] File1, nint[] File2) offsets,
        T errorValue,
        [CallerMemberName] string methodName = ""
    ) where T : unmanaged
        => ReadValueFromOffsets(offsets.File1, offsets.File2, methodName, errorValue);

    protected T ReadSlotValue<T>(
        int slotIndex,
        (nint[] File1, nint[] File2) offsets,
        T errorValue,
        [CallerMemberName] string methodName = ""
    ) where T : unmanaged
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
    ) where T : unmanaged
    {
        if (!TryComputeLobbyAddress(slotIndex, offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Logger.LogCritical("Failed to read slot value for method '{MethodName}' due to address computation error: {ErrorMessage}", methodName, errorMessage);
            return errorValue;
        }

        T result = Read<T>(address);
        Logger.LogDebug("[{MethodName}] Read value of type {Type} at address 0x{Address:X} for slot {SlotIndex}. Value: {Result}", methodName, typeof(T).Name, address, slotIndex, result);
        return result;
    }

    protected T ReadValue<T>(
        nint basePtr,
        ReadOnlySpan<nint> offsets = default,
        [CallerMemberName] string methodName = ""
    ) where T : unmanaged
    {
        nint address = ComputeAddress(basePtr, offsets);
        T result = Read<T>(address);
        Logger.LogDebug("[{MethodName}] Read value of type {Type} at address 0x{Address:X}. Value: {Result}", methodName, typeof(T).Name, address, result);
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
            Logger.LogCritical("Failed to read slot string for method '{MethodName}' due to address computation error: {ErrorMessage}", methodName, errorMessage);
            return errorValue;
        }

        string result = ReadString(address);
        Logger.LogDebug("[{MethodName}] Read string at address 0x{Address:X} for slot {SlotIndex}. Value: '{Result}'", methodName, address, slotIndex, result);
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
            errorMessage = $"[{methodName}] Invalid slot index provided: {slotIndex}. Expected range: 0 to {GameConstants.MaxLobbySlots - 1}.";
            Logger.LogWarning("[{MethodName}] Invalid slot index provided: {SlotIndex}. Expected range: 0 to {MaxLobbySlots}", methodName, slotIndex, GameConstants.MaxLobbySlots - 1);
            return false;
        }

        nint[] offsets = GetOffsets(offsetsFile1, offsetsFile2);
        nint basePtr = GetLobbyBasePointer(slotIndex);

        if (!basePtr.IsNeitherNullNorNegative())
        {
            errorMessage = $"[{methodName}] Failed to obtain a valid base pointer for slot index {slotIndex}. Current game file: {CurrentFile}.";
            Logger.LogWarning("[{MethodName}] Failed to obtain a valid base pointer for slot index {SlotIndex}. Current game file: {CurrentFile}", methodName, slotIndex, CurrentFile);
            return false;
        }

        address = ComputeAddress(basePtr, offsets);

        if (address.IsNeitherNullNorNegative())
        {
            Logger.LogTrace("[{MethodName}] Successfully computed lobby address 0x{Address:X} for slot {SlotIndex} using base pointer 0x{BasePointer:X}", methodName, address, slotIndex, basePtr);
            return true;
        }

        errorMessage = $"[{methodName}] Computed address 0x{address:X} for slot index {slotIndex} is invalid (null or negative).";
        Logger.LogWarning("[{MethodName}] Computed address 0x{Address:X} for slot index {SlotIndex} is invalid (null or negative)", methodName, address, slotIndex);
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
        {
            Logger.LogTrace("[{MethodName}] Successfully computed address 0x{Address:X} using offsets for current game file {GameFile}", methodName, address, CurrentFile);
            return true;
        }

        errorMessage = $"[{methodName}] Computed address 0x{address:X} is invalid (null or negative) using provided offsets for current game file {CurrentFile}.";
        Logger.LogWarning("[{MethodName}] Computed address 0x{Address:X} is invalid (null or negative) using provided offsets for current game file {GameFile}", methodName, address, CurrentFile);
        return false;
    }

    protected T ReadValueFromOffsets<T>(nint[] offsetsFile1, nint[] offsetsFile2, string methodName, T errorValue)
        where T : unmanaged
    {
        if (!TryComputeAddress(offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Logger.LogCritical("Failed to read value of type {Type} for method '{MethodName}' due to address computation error: {ErrorMessage}", typeof(T).Name, methodName, errorMessage);
            return errorValue;
        }

        T result = Read<T>(address);
        Logger.LogDebug("[{MethodName}] Read value of type {Type} at address 0x{Address:X}. Value: {Result}", methodName, typeof(T).Name, address, result);
        return result;
    }

    protected string ReadStringFromOffsets(nint[] offsetsFile1, nint[] offsetsFile2, string methodName, string errorValue)
    {
        if (!TryComputeAddress(offsetsFile1, offsetsFile2, methodName, out nint address, out string? errorMessage))
        {
            Logger.LogCritical("Failed to read string for method '{MethodName}' due to address computation error: {ErrorMessage}", methodName, errorMessage);
            return errorValue;
        }

        string result = ReadString(address);
        Logger.LogDebug("[{MethodName}] Read string at address 0x{Address:X}. Value: '{Result}'", methodName, address, result);
        return result;
    }

    protected string GetScenarioString<TFileOne, TFileTwo>(short scenarioId, TFileOne defaultFileOne, TFileTwo defaultFileTwo)
        where TFileOne : struct, Enum
        where TFileTwo : struct, Enum
    {
        string scenarioName = CurrentFile switch
        {
            GameFile.FileOne => EnumUtility.GetEnumString(scenarioId, defaultFileOne),
            GameFile.FileTwo => EnumUtility.GetEnumString(scenarioId, defaultFileTwo),
            _ => throw new InvalidOperationException($"[{nameof(GetScenarioString)}] Unrecognized game file '{CurrentFile}'. Cannot determine scenario string.")
        };

        Logger.LogDebug("[{MethodName}] Retrieved scenario string '{ScenarioName}' for ID {ScenarioId} with game file {GameFile}", nameof(GetScenarioString), scenarioName, scenarioId, CurrentFile);
        return scenarioName;
    }

    protected nint[] GetOffsets(nint[] offsetsFile1, nint[] offsetsFile2)
    {
        nint[] offsets = CurrentFile switch
        {
            GameFile.FileOne => offsetsFile1,
            GameFile.FileTwo => offsetsFile2,
            _ => []
        };

        Logger.LogTrace("[{MethodName}] Selected offsets based on current game file {GameFile}", nameof(GetOffsets), CurrentFile);
        return offsets;
    }

    protected nint GetLobbyBasePointer(int slotIndex)
    {
        nint basePointer = CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyAddress(slotIndex),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyAddress(slotIndex),
            _ => nint.Zero
        };

        Logger.LogTrace("[{MethodName}] Determined lobby base pointer 0x{BasePointer:X} for slot {SlotIndex} with game file {GameFile}", nameof(GetLobbyBasePointer), basePointer, slotIndex, CurrentFile);
        return basePointer;
    }

    protected nint GetLobbyRoomPlayerBasePointer(int characterId)
    {
        nint basePointer = CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyRoomPlayerAddress(characterId),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyRoomPlayerAddress(characterId),
            _ => nint.Zero
        };

        Logger.LogTrace("[{MethodName}] Determined lobby room player base pointer 0x{BasePointer:X} for character ID {CharacterId} with game file {GameFile}", nameof(GetLobbyRoomPlayerBasePointer), basePointer, characterId, CurrentFile);
        return basePointer;
    }

    protected nint ComputeAddress(nint basePtr, params ReadOnlySpan<nint> offsets)
    {
        nint computedAddress = offsets.Length is 0
            ? _eEmemMemory.GetAddressFromPtr(basePtr)
            : _eEmemMemory.GetAddressFromPtrChain(basePtr, offsets);

        Logger.LogTrace("[{MethodName}] Computed address 0x{ComputedAddress:X} from base pointer 0x{BasePointer:X} with {OffsetCount} offsets", nameof(ComputeAddress), computedAddress, basePtr, offsets.Length);
        return computedAddress;
    }

    protected nint ComputeAddress(params ReadOnlySpan<nint> offsets)
    {
        nint computedAddress = offsets.Length is 1
            ? _eEmemMemory.GetAddressFromPtr(offsets[0])
            : _eEmemMemory.GetAddressFromPtrChain(offsets[0], offsets);

        Logger.LogTrace("[{MethodName}] Computed address 0x{ComputedAddress:X} from {OffsetCount} offsets (first offset: 0x{FirstOffset:X})", nameof(ComputeAddress), computedAddress, offsets.Length, offsets.Length > 0 ? offsets[0] : 0);
        return computedAddress;
    }
}