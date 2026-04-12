using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.UnitTests;

public sealed class ReaderBaseStringReadTests
{
    [Test]
    public async Task ReadString_ReturnsDecodedValue_WhenUnderlyingReaderSucceeds()
    {
        FakeStringReader stringReader = new(success: true, value: "Kevin");
        using TestReader reader = CreateReader(stringReader);

        string result = reader.ReadConfiguredString("<error>");

        await Assert.That(result).IsEqualTo("Kevin");
        await Assert.That(stringReader.CallCount).IsEqualTo(1);
        await Assert.That(stringReader.LastAddress).IsEqualTo(TestReader.TargetStringAddress);
    }

    [Test]
    public async Task ReadString_ReturnsCallerErrorValue_WhenUnderlyingReaderFails()
    {
        FakeStringReader stringReader = new(success: false, value: string.Empty);
        using TestReader reader = CreateReader(stringReader);

        string result = reader.ReadConfiguredString("<error>");

        await Assert.That(result).IsEqualTo("<error>");
        await Assert.That(stringReader.CallCount).IsEqualTo(1);
        await Assert.That(stringReader.LastAddress).IsEqualTo(TestReader.TargetStringAddress);
    }

    private static TestReader CreateReader(FakeStringReader stringReader) =>
        new(new FakeGameClient(), new FakeEEmemAddressReader(stringReader));

    private sealed class TestReader(IGameClient gameClient, IEEmemAddressReader eememReader)
        : ReaderBase(gameClient, eememReader, NullLogger.Instance)
    {
        internal const nint TargetStringAddress = 0x1234;

        public string ReadConfiguredString(string errorValue) => ReadString([TargetStringAddress], [], errorValue);
    }

    private sealed class FakeStringReader(bool success, string value) : IStringReader
    {
        public int CallCount { get; private set; }

        public nint LastAddress { get; private set; }

        public bool TryRead(nint hProcess, nint address, out string result, Encoding? encoding = null)
        {
            CallCount++;
            LastAddress = address;
            result = value;
            return success;
        }

        public string Read(nint hProcess, nint address, Encoding? encoding = null) =>
            TryRead(hProcess, address, out string result, encoding) ? result : string.Empty;
    }

    private sealed class FakeEEmemAddressReader(FakeStringReader stringReader) : IEEmemAddressReader
    {
        public ISafeMemoryReader MemoryReader { get; } = new FakeSafeMemoryReader();

        public IStringReader StringReader { get; } = stringReader;

        public nint GetAddressFromPtr(nint ptrOffset) => ptrOffset;

        public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets)
        {
            nint result = ptrOffset;
            foreach (nint offset in offsets)
                result += offset;

            return result;
        }

        public bool IsAddressInBounds(nint address) => address != nint.Zero;
    }

    private sealed class FakeSafeMemoryReader : ISafeMemoryReader
    {
        public T Read<T>(nint hProcess, nint address)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte))
            {
                byte value = address == FileOnePtrs.DiscStart ? (byte)0x53 : (byte)0x00;
                return (T)(object)value;
            }

            return default;
        }

        public T ReadStruct<T>(nint hProcess, nint address)
            where T : struct => default;
    }

    private sealed class FakeGameClient : IGameClient
    {
        public nint Handle => (nint)42;

        public bool IsAttached => true;

        public Process? Process => null;

        [SupportedOSPlatform("windows")]
        public nint MainModuleBase => nint.Zero;

        public void Dispose() { }
    }
}
