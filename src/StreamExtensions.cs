using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Lobby;

public static class StreamExtensions
{
    public static unsafe T Read<T>(this Stream stream) where T : unmanaged
    {
        Span<byte> buffer = stackalloc byte[sizeof(T)];
        stream.ReadExactly(buffer);
        return MemoryMarshal.Read<T>(buffer);
    }

    public static unsafe void Write<T>(this Stream stream, in T value) where T : unmanaged
    {
        Span<byte> buffer = stackalloc byte[sizeof(T)];
        MemoryMarshal.Write(buffer, value);
        stream.Write(buffer);
    }

    // Read and write methods for byte
    public static byte ReadByte(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        stream.Read(buffer);
        return buffer[0];
    }

    public static void WriteByte(this Stream stream, byte value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        buffer[0] = value;
        stream.Write(buffer);
    }

    // Read and write methods for short (Int16)
    public static short ReadInt16(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        stream.Read(buffer);
        return BinaryPrimitives.ReadInt16LittleEndian(buffer);
    }

    public static void WriteInt16(this Stream stream, short value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    // Read and write methods for ushort (UInt16)
    public static ushort ReadUInt16(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        stream.Read(buffer);
        return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
    }

    public static void WriteUInt16(this Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    // Read and write methods for int (Int32)
    public static int ReadInt32(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        stream.Read(buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    public static void WriteInt32(this Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    // Read and write methods for uint (UInt32)
    public static uint ReadUInt32(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        stream.Read(buffer);
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    public static void WriteUInt32(this Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }
}