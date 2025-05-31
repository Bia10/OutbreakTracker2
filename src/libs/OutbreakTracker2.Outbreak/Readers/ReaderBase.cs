using Microsoft.Extensions.Logging;
using OutbreakTracker2.Extensions;
using OutbreakTracker2.Memory.MemoryReader;
using OutbreakTracker2.Memory.StringReader;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Extensions;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace OutbreakTracker2.Outbreak.Readers;

public abstract class ReaderBase : IDisposable
{
    protected readonly ILogger Logger;
    private readonly GameClient _gameClient;
    private readonly IEEmemMemory _eememMemory;
    private readonly ISafeMemoryReader _memoryReader;
    private readonly IStringReader _stringReader;

    protected GameFile CurrentFile { get; }

    private bool _disposed;

    protected ReaderBase(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
    {
        _gameClient = gameClient ?? throw new ArgumentNullException(nameof(gameClient));
        _eememMemory = eememMemory ?? throw new ArgumentNullException(nameof(eememMemory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _memoryReader = eememMemory.MemoryReader;
        _stringReader = eememMemory.StringReader;

        CurrentFile = GetGameFile();
    }

    private T Read<T>(
        nint address,
        [CallerMemberName] string methodName = "")
        where T : unmanaged
    {
        if (address == nint.Zero)
        {
            Logger.LogWarning("[{MethodName}] Attempted to read {Type} from a null address.",
                methodName, typeof(T).Name);
            return default;
        }

        try
        {
            T result = _memoryReader.Read<T>(_gameClient.Handle, address);
            Logger.LogTrace("[{MethodName}] Successfully read {Type} from address 0x{Address:X}. Value: {Result}",
                methodName, typeof(T).Name, address, result);
            return result;
        }
        catch (AccessViolationException ex)
        {
            Logger.LogError(ex, "[{MethodName}] Access violation reading {Type} from address 0x{Address:X}",
                methodName, typeof(T).Name, address);
            return default;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{MethodName}] Unexpected error reading {Type} from address 0x{Address:X}",
                methodName, typeof(T).Name, address);
            return default;
        }
    }

    private string ReadString(
        nint address,
        Encoding? encoding = null,
        [CallerMemberName] string methodName = "")
    {
        if (address == nint.Zero)
        {
            Logger.LogWarning("[{MethodName}] Attempted to read string from a null address.", methodName);
            return string.Empty;
        }

        try
        {
            string result = _stringReader.Read(_gameClient.Handle, address, encoding);
            Logger.LogTrace("[{MethodName}] Successfully read string from address 0x{Address:X}. Value: '{Result}'",
                methodName, address, result);
            return result;
        }
        catch (AccessViolationException ex)
        {
            Logger.LogError(ex, "[{MethodName}] Access violation reading string from address 0x{Address:X}",
                methodName, address);
            return string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{MethodName}] Unexpected error reading string from address 0x{Address:X}",
                methodName, address);
            return string.Empty;
        }
    }

    private GameFile GetGameFile()
    {
        byte f1Byte = Read<byte>(_eememMemory.GetAddressFromPtr(FileOnePtrs.DiscStart));
        byte f2Byte = Read<byte>(_eememMemory.GetAddressFromPtr(FileTwoPtrs.DiscStart));

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

    private nint ComputeAddress(
        nint basePtr,
        ReadOnlySpan<nint> offsets = default,
        [CallerMemberName] string methodName = "")
    {
        if (basePtr == nint.Zero)
        {
            Logger.LogWarning("[{MethodName}] Attempted to compute address with a null base pointer.", methodName);
            return nint.Zero;
        }

        nint computedAddress = offsets.IsEmpty
            ? _eememMemory.GetAddressFromPtr(basePtr)
            : _eememMemory.GetAddressFromPtrChain(basePtr, offsets);

        if (computedAddress == nint.Zero && !offsets.IsEmpty)
        {
            Logger.LogWarning("[{MethodName}] Computed address is null (0x{ComputedAddress:X}) from base pointer 0x{BasePointer:X} with {OffsetCount} offsets. This may indicate an issue in the pointer chain.",
                methodName, computedAddress, basePtr, offsets.Length);
        }
        else
        {
            Logger.LogTrace("[{MethodName}] Computed address 0x{ComputedAddress:X} from base pointer 0x{BasePointer:X} with {OffsetCount} offsets",
                methodName, computedAddress, basePtr, offsets.Length);
        }

        return computedAddress;
    }

    private nint ComputeAddress(
        ReadOnlySpan<nint> offsets,
        [CallerMemberName] string methodName = "")
    {
        if (offsets.IsEmpty)
        {
            Logger.LogWarning("[{MethodName}] Attempted to compute address with an empty offset list.", methodName);
            return nint.Zero;
        }

        nint computedAddress = offsets.Length is 1
            ? _eememMemory.GetAddressFromPtr(offsets[0])
            : _eememMemory.GetAddressFromPtrChain(offsets[0], offsets[1..]);

        if (computedAddress == nint.Zero)
        {
            Logger.LogWarning("[{MethodName}] Computed address is null (0x{ComputedAddress:X}) from {OffsetCount} offsets (first offset: 0x{FirstOffset:X}). This may indicate an issue in the pointer chain.",
                methodName, computedAddress, offsets.Length, offsets[0]);
        }
        else
        {
            Logger.LogTrace("[{MethodName}] Computed address 0x{ComputedAddress:X} from {OffsetCount} offsets (first offset: 0x{FirstOffset:X})",
                methodName, computedAddress, offsets.Length, offsets[0]);
        }

        return computedAddress;
    }

    protected ReadOnlySpan<nint> GetFileSpecificOffsets(
        (nint[] File1, nint[] File2) offsetTuple,
        [CallerMemberName] string methodName = "")
    {
        ReadOnlySpan<nint> offsets = CurrentFile switch
        {
            GameFile.FileOne => offsetTuple.File1,
            GameFile.FileTwo => offsetTuple.File2,
            _ => null!
        };

        if (CurrentFile is GameFile.Unknown)
        {
            Logger.LogWarning("[{MethodName}] GetFileSpecificOffsets called with CurrentFile set to Unknown. Returning empty offsets array.",
                methodName);
        }
        else
        {
            Logger.LogTrace("[{MethodName}] Selected offsets based on current game file {GameFile}",
                methodName, CurrentFile);
        }

        return offsets;
    }

    protected nint GetFileSpecificSingleNintOffset((nint File1, nint File2) offsetTuple)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => offsetTuple.File1,
            GameFile.FileTwo => offsetTuple.File2,
            _ => nint.Zero
        };
    }

    protected int GetFileSpecificSingleIntOffset((int File1, int File2) offsetTuple)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => offsetTuple.File1,
            GameFile.FileTwo => offsetTuple.File2,
            _ => 0
        };
    }

    private nint GetLobbyBasePointer(
        int slotIndex,
        [CallerMemberName] string methodName = "")
    {
        if (!slotIndex.IsSlotIndexValid())
        {
            Logger.LogWarning("[{MethodName}] Invalid slot index provided: {SlotIndex}. Expected range: 0 to {MaxLobbySlots}",
                methodName, slotIndex, GameConstants.MaxLobbySlots - 1);
            return nint.Zero;
        }

        nint basePointer = CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyAddress(slotIndex),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyAddress(slotIndex),
            _ => nint.Zero
        };

        if (basePointer == nint.Zero)
            Logger.LogWarning(
                "[{MethodName}] Failed to obtain a valid base pointer for slot index {SlotIndex}. Current game file: {CurrentFile}",
                methodName, slotIndex, CurrentFile);
        else
            Logger.LogTrace(
                "[{MethodName}] Determined lobby base pointer 0x{BasePointer:X} for slot {SlotIndex} with game file {GameFile}",
                methodName, basePointer, slotIndex, CurrentFile);

        return basePointer;
    }

    protected nint GetLobbyRoomPlayerBasePointer(
        int characterId,
        [CallerMemberName] string methodName = "")
    {
        nint basePointer = CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyRoomPlayerAddress(characterId),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyRoomPlayerAddress(characterId),
            _ => nint.Zero
        };

        if (basePointer == nint.Zero)
        {
            Logger.LogWarning("[{MethodName}] Failed to obtain a valid lobby room player base pointer for character ID {CharacterId}. Current game file: {CurrentFile}",
                methodName, characterId, CurrentFile);
        }
        else
        {
            Logger.LogTrace("[{MethodName}] Determined lobby room player base pointer 0x{BasePointer:X} for character ID {CharacterId} with game file {GameFile}",
                methodName, basePointer, characterId, CurrentFile);
        }

        return basePointer;
    }

    private bool TryComputeLobbyAddress(
        int slotIndex, ReadOnlySpan<nint> offsetsFile1,
        ReadOnlySpan<nint> offsetsFile2,
        out nint address,
        [CallerMemberName] string methodName = "")
    {
        address = nint.Zero;

        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets((offsetsFile1.ToArray(), offsetsFile2.ToArray()), methodName);

        nint basePtr = GetLobbyBasePointer(slotIndex, methodName);

        if (basePtr == nint.Zero)
            return false;

        address = ComputeAddress(basePtr, offsets, methodName);

        if (!address.IsNeitherNullNorNegative())
            return false;

        Logger.LogTrace("[{MethodName}] Successfully computed lobby address 0x{Address:X} for slot {SlotIndex} using base pointer 0x{BasePointer:X}",
            methodName, address, slotIndex, basePtr);
        return true;
    }

    private bool TryComputeAddress(
        ReadOnlySpan<nint> offsetsFile1,
        ReadOnlySpan<nint> offsetsFile2,
        out nint address,
        [NotNullWhen(false)] out string? errorMessage,
        [CallerMemberName] string methodName = "")
    {
        address = nint.Zero;
        errorMessage = null;

        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets((offsetsFile1.ToArray(), offsetsFile2.ToArray()), methodName);
        if (offsets.IsEmpty)
        {
            errorMessage = $"[{methodName}] Selected offsets for current game file {CurrentFile} are empty.";
            return false;
        }

        address = ComputeAddress(offsets, methodName);

        if (address.IsNeitherNullNorNegative())
        {
            Logger.LogTrace("[{MethodName}] Successfully computed address 0x{Address:X} using offsets for current game file {GameFile}",
                methodName, address, CurrentFile);
            return true;
        }

        errorMessage = $"[{methodName}] Computed address 0x{address:X} is invalid (null or negative) using provided offsets for current game file {CurrentFile}.";
        return false;
    }

    private TResult ReadValueFromOffsetsInternal<TResult>(
        ReadOnlySpan<nint> offsetsFile1, ReadOnlySpan<nint> offsetsFile2,
        TResult errorValue,
        Func<nint, TResult> readOperation,
        [CallerMemberName] string methodName = "")
    {
        if (!TryComputeAddress(offsetsFile1, offsetsFile2, out nint address, out string? errorMessage, methodName))
        {
            Logger.LogError("Failed to read {Type} for method '{MethodName}' due to address computation error: {ErrorMessage}",
                typeof(TResult).Name, methodName, errorMessage);
            return errorValue;
        }

        TResult result = readOperation(address);

        if (EqualityComparer<TResult>.Default.Equals(result, errorValue) && address != nint.Zero) // Check if errorValue is default AND address was valid
        {
            Logger.LogDebug("[{MethodName}] Read operation for {Type} at address 0x{Address:X} returned error value.",
                methodName, typeof(TResult).Name, address);
        }
        else if (address == nint.Zero)
        {
            Logger.LogWarning("[{MethodName}] Read operation for {Type} failed because address was null (0x{Address:X}).",
                methodName, typeof(TResult).Name, address);
        }
        else
        {
            Logger.LogDebug("[{MethodName}] Read {Type} at address 0x{Address:X}. Value: {Result}",
                methodName, typeof(TResult).Name, address, result);
        }

        return result;
    }

    protected T ReadValue<T>(
        ReadOnlySpan<nint> offsetsFile1, ReadOnlySpan<nint> offsetsFile2,
        T errorValue,
        [CallerMemberName] string methodName = ""
    ) where T : unmanaged
        => ReadValueFromOffsetsInternal(offsetsFile1, offsetsFile2, errorValue,
            readOperation: address => Read<T>(address, methodName), methodName);

    protected string ReadString(
        ReadOnlySpan<nint> offsetsFile1, ReadOnlySpan<nint> offsetsFile2,
        string errorValue,
        [CallerMemberName] string methodName = ""
    ) => ReadValueFromOffsetsInternal(offsetsFile1, offsetsFile2, errorValue,
        readOperation: address => ReadString(address, null, methodName), methodName);

    private TResult ReadSlotValueInternal<TResult>(
        int slotIndex,
        ReadOnlySpan<nint> offsetsFile1, ReadOnlySpan<nint> offsetsFile2,
        TResult errorValue,
        Func<nint, TResult> readOperation,
        [CallerMemberName] string methodName = "")
    {
        if (!TryComputeLobbyAddress(slotIndex, offsetsFile1, offsetsFile2, out nint address, methodName))
            return errorValue;

        TResult result = readOperation(address);

        if (EqualityComparer<TResult>.Default.Equals(result, errorValue) && address != nint.Zero)
        {
            Logger.LogDebug("[{MethodName}] Read operation for {Type} at address 0x{Address:X} for slot {SlotIndex} returned error value.",
                methodName, typeof(TResult).Name, address, slotIndex);
        }
        else if (address == nint.Zero)
        {
            Logger.LogWarning("[{MethodName}] Read operation for {Type} failed for slot {SlotIndex} because address was null (0x{Address:X}).",
                methodName, typeof(TResult).Name, slotIndex, address);
        }
        else
        {
            Logger.LogDebug("[{MethodName}] Read {Type} at address 0x{Address:X} for slot {SlotIndex}. Value: {Result}",
                methodName, typeof(TResult).Name, address, slotIndex, result);
        }

        return result;
    }

    protected T ReadSlotValue<T>(
        int slotIndex,
        ReadOnlySpan<nint> offsetsFile1, ReadOnlySpan<nint> offsetsFile2,
        T errorValue,
        [CallerMemberName] string methodName = ""
    ) where T : unmanaged
        => ReadSlotValueInternal(slotIndex, offsetsFile1, offsetsFile2, errorValue,
            readOperation: address => Read<T>(address, methodName), methodName);

    protected string ReadSlotString(
        int slotIndex,
        ReadOnlySpan<nint> offsetsFile1, ReadOnlySpan<nint> offsetsFile2,
        string errorValue,
        [CallerMemberName] string methodName = ""
    ) => ReadSlotValueInternal(slotIndex, offsetsFile1, offsetsFile2, errorValue,
        readOperation: address => ReadString(address, null, methodName), methodName);

    protected T ReadValue<T>(
        nint basePtr,
        ReadOnlySpan<nint> offsets = default,
        [CallerMemberName] string methodName = ""
    ) where T : unmanaged
    {
        nint address = ComputeAddress(basePtr, offsets, methodName);
        T result = Read<T>(address, methodName);

        if (EqualityComparer<T>.Default.Equals(result, default) && address != nint.Zero)
        {
            Logger.LogDebug("[{MethodName}] Read value of type {Type} at address 0x{Address:X} returned default value. This might indicate a read error.",
                methodName, typeof(T).Name, address);
        }

        return result;
    }

    protected string GetScenarioString<TFileOne, TFileTwo>(
        short scenarioId,
        TFileOne defaultFileOne,
        TFileTwo defaultFileTwo)
        where TFileOne : struct, Enum
        where TFileTwo : struct, Enum
    {
        string scenarioName = CurrentFile switch
        {
            GameFile.FileOne => EnumUtility.GetEnumString(scenarioId, defaultFileOne),
            GameFile.FileTwo => EnumUtility.GetEnumString(scenarioId, defaultFileTwo),
            _ => "Unknown Scenario"
        };

        if (string.IsNullOrEmpty(scenarioName) || scenarioName.Equals("Unknown Scenario", StringComparison.Ordinal))
        {
            Logger.LogError("[{MethodName}] Unrecognized game file '{CurrentFile}' or invalid scenario ID {ScenarioId}. Cannot determine scenario string.",
                nameof(GetScenarioString), CurrentFile, scenarioId);
            return "Unknown Scenario";
        }

        Logger.LogDebug("[{MethodName}] Retrieved scenario string '{ScenarioName}' for ID {ScenarioId} with game file {GameFile}",
            nameof(GetScenarioString), scenarioName, scenarioId, CurrentFile);
        return scenarioName;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            if (_gameClient is IDisposable disposableGameClient)
            {
                disposableGameClient.Dispose();
                Logger.LogDebug("Disposed GameClient.");
            }
        }

        _disposed = true;
        Logger.LogTrace("ReaderBase disposed.");
    }
}