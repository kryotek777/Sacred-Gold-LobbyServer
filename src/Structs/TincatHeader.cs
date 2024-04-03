using System.Runtime.InteropServices;

namespace Sacred.Types;

[StructLayout(LayoutKind.Explicit, Size = HeaderSize)]
public unsafe struct TincatHeader
{
    public const uint TincatMagic = 0xDABAFBEF;
    public const int HeaderSize = 0x1C;

    [FieldOffset(0)]
    public uint magic;
    [FieldOffset(4)]
    public uint from;
    [FieldOffset(8)]
    public uint to;
    [FieldOffset(12)]
    public TincatMsgType msgType;
    [FieldOffset(16)]
    public uint unknown;
    [FieldOffset(20)]
    public int dataLength;
    [FieldOffset(24)]
    public uint crc32;

    [FieldOffset(0)]
    public fixed byte rawData[HeaderSize];

    public Span<byte> AsSpan()
    {
        fixed(byte* p = rawData)
            return new Span<byte>(p, HeaderSize);
    }
}