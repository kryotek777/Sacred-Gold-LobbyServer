using System.Runtime.InteropServices;

namespace Sacred;

public ref struct SpanReader
{
    public ReadOnlySpan<byte> Span { get; private init; }
    public int Position { get; set; }
    
    public SpanReader(ReadOnlySpan<byte> Span)
    {
        this.Span = Span;
        Position = 0;
    }

    public unsafe T Read<T>() where T : unmanaged
    {
        var result = MemoryMarshal.Read<T>(Span.Slice(Position));
        Position += sizeof(T);
        return result;
    }

    public bool ReadBoolean()
    {
        var result = Span[Position] != 0;
        Position += sizeof(bool);
        return result;    
    }

    public byte ReadByte()
    {
        var result = Span[Position];
        Position += sizeof(byte);
        return result;
    }

    public short ReadInt16()
    {
        var result = BitConverter.ToInt16(Span.Slice(Position));
        Position += sizeof(short);
        return result;
    }

    public ushort ReadUInt16()
    {
        var result = BitConverter.ToUInt16(Span.Slice(Position));
        Position += sizeof(ushort);
        return result;
    }

    public int ReadInt32()
    {
        var result = BitConverter.ToInt32(Span.Slice(Position));
        Position += sizeof(int);
        return result;
    }

    public uint ReadUInt32()
    {
        var result = BitConverter.ToUInt32(Span.Slice(Position));
        Position += sizeof(uint);
        return result;
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var result = Span.Slice(Position, count);
        Position += count;
        return result;
    }

    public ReadOnlySpan<byte> ReadAll()
    {
        var result = Span.Slice(Position);
        Position = Span.Length;
        return result;
    }

    public static implicit operator SpanReader(ReadOnlySpan<byte> Span) => new(Span);
    public static implicit operator SpanReader(Span<byte> Span) => new(Span);
}
