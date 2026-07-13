using Cloudflare.Workers;
using System;
using System.Threading.Tasks;

/// <summary>
/// Sample worker demonstrating Unsafe code simulation
/// Note: Uses ArrayBuffer and typed arrays to simulate pointer operations
/// </summary>
public class UnsafeSample
{
    public async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var url = new URL(request.Url);
        
        if (url.Pathname == "/memory")
        {
            return await DemonstrateMemoryOperations();
        }
        
        if (url.Pathname == "/buffer")
        {
            return await DemonstrateBufferOperations();
        }
        
        return Response.Json(new { 
            message = "Unsafe Code Sample",
            endpoints = new[] { "/memory", "/buffer" }
        });
    }

    private async Task<Response> DemonstrateMemoryOperations()
    {
        // Allocate memory block (simulated with ArrayBuffer)
        using var memory = UnsafeMemory.Allocate(1024); // 1KB
        
        // Write data at specific offsets
        memory.WriteInt32(0, 42);
        memory.WriteInt32(4, 100);
        memory.WriteByte(8, 255);
        
        // Read data back
        var value1 = memory.ReadInt32(0);
        var value2 = memory.ReadInt32(4);
        var byteValue = memory.ReadByte(8);
        
        // Copy memory region
        memory.Copy(0, 16, 8); // Copy 8 bytes from offset 0 to offset 16
        
        var copiedValue1 = memory.ReadInt32(16);
        var copiedValue2 = memory.ReadInt32(20);
        
        // Get memory dump
        var dump = memory.ToBase64();
        
        return Response.Json(new
        {
            message = "Memory operations completed",
            writtenValues = new { value1, value2, byteValue },
            copiedValues = new { copiedValue1, copiedValue2 },
            allocatedSize = memory.Size,
            memoryDump = dump.Substring(0, Math.Min(64, dump.Length)) + "..."
        });
    }

    private async Task<Response> DemonstrateBufferOperations()
    {
        // Create buffer with initial data
        var buffer = UnsafeBuffer.FromString("Hello, Cloudflare Workers!");
        
        // Access individual bytes
        var firstByte = buffer[0]; // 'H'
        var lastByte = buffer[buffer.Length - 1]; // '!'
        
        // Modify buffer
        buffer[0] = (byte)'h'; // lowercase h
        
        // Convert back to string
        var modifiedString = buffer.ToString();
        
        // Buffer arithmetic (simulated pointer arithmetic)
        var subBuffer = buffer.Slice(7, 10); // "Cloudflare"
        
        // Concatenate buffers
        var extraBuffer = UnsafeBuffer.FromString(" - Extended");
        var combined = UnsafeBuffer.Concat(buffer, extraBuffer);
        
        return Response.Json(new
        {
            message = "Buffer operations completed",
            originalLength = "Hello, Cloudflare Workers!".Length,
            firstByte,
            lastByte,
            modifiedString,
            subBufferContent = subBuffer.ToString(),
            combinedLength = combined.Length,
            combinedContent = combined.ToString()
        });
    }
}

/// <summary>
/// Simulated unsafe memory operations using ArrayBuffer
/// </summary>
public sealed class UnsafeMemory : IDisposable
{
    private readonly byte[] _buffer;
    private bool _disposed;

    private UnsafeMemory(int size)
    {
        _buffer = new byte[size];
        Size = size;
    }

    public int Size { get; }

    public static UnsafeMemory Allocate(int size)
    {
        return new UnsafeMemory(size);
    }

    public void WriteInt32(int offset, int value)
    {
        CheckBounds(offset, 4);
        _buffer[offset] = (byte)(value & 0xFF);
        _buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        _buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        _buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    public int ReadInt32(int offset)
    {
        CheckBounds(offset, 4);
        return _buffer[offset] |
               (_buffer[offset + 1] << 8) |
               (_buffer[offset + 2] << 16) |
               (_buffer[offset + 3] << 24);
    }

    public void WriteByte(int offset, byte value)
    {
        CheckBounds(offset, 1);
        _buffer[offset] = value;
    }

    public byte ReadByte(int offset)
    {
        CheckBounds(offset, 1);
        return _buffer[offset];
    }

    public void Copy(int sourceOffset, int destOffset, int length)
    {
        CheckBounds(sourceOffset, length);
        CheckBounds(destOffset, length);
        Array.Copy(_buffer, sourceOffset, _buffer, destOffset, length);
    }

    public string ToBase64()
    {
        return Convert.ToBase64String(_buffer);
    }

    private void CheckBounds(int offset, int length)
    {
        if (offset < 0 || offset + length > Size)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Access out of bounds");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Clear memory for security
            Array.Clear(_buffer, 0, Size);
            _disposed = true;
        }
    }
}

/// <summary>
/// Simulated unsafe buffer operations
/// </summary>
public sealed class UnsafeBuffer
{
    private readonly byte[] _data;

    private UnsafeBuffer(byte[] data)
    {
        _data = data;
    }

    public int Length => _data.Length;

    public byte this[int index]
    {
        get => _data[index];
        set => _data[index] = value;
    }

    public static UnsafeBuffer FromString(string str)
    {
        return new UnsafeBuffer(System.Text.Encoding.UTF8.GetBytes(str));
    }

    public override string ToString()
    {
        return System.Text.Encoding.UTF8.GetString(_data);
    }

    public UnsafeBuffer Slice(int start, int length)
    {
        var sliced = new byte[length];
        Array.Copy(_data, start, sliced, 0, length);
        return new UnsafeBuffer(sliced);
    }

    public static UnsafeBuffer Concat(UnsafeBuffer left, UnsafeBuffer right)
    {
        var combined = new byte[left.Length + right.Length];
        Array.Copy(left._data, 0, combined, 0, left.Length);
        Array.Copy(right._data, 0, combined, left.Length, right.Length);
        return new UnsafeBuffer(combined);
    }
}

/// <summary>
/// Environment bindings
/// </summary>
public class Env
{
}
