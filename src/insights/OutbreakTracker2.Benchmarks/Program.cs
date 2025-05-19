using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OutbreakTracker2.Benchmarks;

internal static class SafeMemoryReader
{
    internal static T Read<T>(nint hProcess, nint address) where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();
        switch (size)
        {
            case < 0: throw new InvalidOperationException($"Size of T cannot be negative. Size: {size}");
            case 0: return default;
            default:
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

                    try
                    {
                        if (!Kernel32.ReadProcessMemory_Safe(hProcess, address, buffer, size, out int bytesRead))
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        if (bytesRead != size)
                            throw new InvalidOperationException($"Failed to read the expected number of bytes. Read: {bytesRead}, Expected: {size}");

                        Span<byte> bufferSpan = buffer.AsSpan(0, size);
                        ref T result = ref MemoryMarshal.Cast<byte, T>(bufferSpan)[0];

                        return result;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
        }
    }
}

internal static class UnsafeMemoryReader
{
    private const int StackAllocThreshold = 8192;

    internal static unsafe T Read<T>(nint hProcess, nint address) where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        switch (size)
        {
            case < 0: throw new InvalidOperationException($"Size of T cannot be negative. Size: {size}");
            case 0: return default;
            default:
                {
                    byte[]? arrayPoolBuffer = null;

                    try
                    {
                        byte* bufferPtr;
                        if (size <= StackAllocThreshold)
                        {
                            byte* stackAllocatedBuffer = stackalloc byte[size];
                            bufferPtr = stackAllocatedBuffer;

                            if (!Kernel32.ReadProcessMemory_Unsafe(hProcess, address, bufferPtr, size, out int bytesRead))
                                throw new Win32Exception(Marshal.GetLastWin32Error());
                            if (bytesRead != size)
                                throw new InvalidOperationException($"Failed to read the expected number of bytes. Read: {bytesRead}, Expected: {size}");

                            return *(T*)bufferPtr;
                        }
                        else
                        {
                            arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(size);

                            fixed (byte* pinnedPtr = arrayPoolBuffer)
                            {
                                bufferPtr = pinnedPtr;

                                if (!Kernel32.ReadProcessMemory_Unsafe(hProcess, address, bufferPtr, size, out int bytesRead))
                                    throw new Win32Exception(Marshal.GetLastWin32Error());
                                if (bytesRead != size)
                                    throw new InvalidOperationException($"Failed to read the expected number of bytes. Read: {bytesRead}, Expected: {size}");

                                return *(T*)bufferPtr;
                            }
                        }
                    }
                    finally
                    {
                        if (arrayPoolBuffer is not null)
                            ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
                    }
                }
        }
    }
}

public struct Struct2Bytes { public byte B1; public byte B2; }

public struct Struct2Ints { public int I1; public int I2; }

public struct Struct3IntsFloat { public int I1; public int I2; public int I3; public float F1; }

public unsafe struct UnmanagedStructBytes8 { public fixed byte Data[8]; }

public unsafe struct UnmanagedStructBytes16 { public fixed byte Data[16]; }

public unsafe struct UnmanagedStructBytes32 { public fixed byte Data[32]; }

public unsafe struct UnmanagedStructBytes100 { public fixed byte Data[100]; }

public unsafe struct UnmanagedStructBytes1500 { public fixed byte Data[1500]; }

public unsafe struct UnmanagedStructBytes5000 { public fixed byte Data[5000]; }

[MemoryDiagnoser]
[SimpleJob(launchCount: 2, warmupCount: 5, iterationCount: 10)]
[GenericTypeArguments(typeof(byte))]
[GenericTypeArguments(typeof(short))]
[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(long))]
[GenericTypeArguments(typeof(float))]
[GenericTypeArguments(typeof(double))]
[GenericTypeArguments(typeof(Struct2Bytes))]
[GenericTypeArguments(typeof(Struct2Ints))]
[GenericTypeArguments(typeof(Struct3IntsFloat))]
[GenericTypeArguments(typeof(UnmanagedStructBytes8))]
[GenericTypeArguments(typeof(UnmanagedStructBytes16))]
[GenericTypeArguments(typeof(UnmanagedStructBytes32))]
[GenericTypeArguments(typeof(UnmanagedStructBytes100))]
[GenericTypeArguments(typeof(UnmanagedStructBytes1500))]
[GenericTypeArguments(typeof(UnmanagedStructBytes5000))]
public class MemoryReadBenchmark<T> where T : unmanaged
{
    private nint _hProcess;
    private GCHandle _gcHandle;
    private nint _address;
    private byte[]? _dataBuffer;
    private int _dataSize;

    [GlobalSetup]
    public void Setup()
    {
        _hProcess = Kernel32.OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, Environment.ProcessId);
        if (_hProcess == nint.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open process handle.");

        _dataSize = Marshal.SizeOf<T>();

        if (_dataSize <= 0)
            return;

        _dataBuffer = new byte[_dataSize];

        for (int i = 0; i < _dataSize; i++)
            _dataBuffer[i] = (byte)(i % 256);

        _gcHandle = GCHandle.Alloc(_dataBuffer, GCHandleType.Pinned);
        _address = _gcHandle.AddrOfPinnedObject();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_gcHandle.IsAllocated)
            _gcHandle.Free();

        _dataBuffer = null;
        _hProcess = nint.Zero;
    }

    [Benchmark(Baseline = true)]
    public T ReadSafe()
        => SafeMemoryReader.Read<T>(_hProcess, _address);

    [Benchmark]
    public T ReadUnsafe()
        => UnsafeMemoryReader.Read<T>(_hProcess, _address);
}

public class Program
{
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}