using System.Runtime.InteropServices;

namespace Sacred;


public ref struct SpanWriter
{
    public Span<byte> Span { get; private init; }
    public int Position { get; set; }
    
    public SpanWriter(Span<byte> Span)
    {
        this.Span = Span;
        Position = 0;
    }

    public unsafe void Write<T>(T value) where T : unmanaged
    {
        MemoryMarshal.Write(Span.Slice(Position), in value);
        Position += sizeof(T);
    }

    public void WriteByte(byte value)
    {
        Span[Position] = value;
        Position += sizeof(byte);
    }

    public void WriteInt16(short value)
    {
        BitConverter.TryWriteBytes(Span.Slice(Position), value);
        Position += sizeof(short);
    }

    public void WriteUInt16(ushort value)
    {
        BitConverter.TryWriteBytes(Span.Slice(Position), value);
        Position += sizeof(ushort);
    }

    public void WriteInt32(int value)
    {
        BitConverter.TryWriteBytes(Span.Slice(Position), value);
        Position += sizeof(int);
    }

    public void WriteUInt32(uint value)
    {
        BitConverter.TryWriteBytes(Span.Slice(Position), value);
        Position += sizeof(uint);
    }

    public void Write(ReadOnlySpan<byte> source)
    {
        source.CopyTo(Span.Slice(Position, source.Length));
        Position += source.Length;
    }

    public static implicit operator SpanWriter(Span<byte> Span) => new(Span);
}
