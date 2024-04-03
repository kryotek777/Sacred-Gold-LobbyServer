using System.Runtime.InteropServices;

namespace Sacred.Types;

[StructLayout(LayoutKind.Explicit, Size = HeaderSize)]
public unsafe struct SacredHeader
{
    public const int SacredMagic = 0x26B6;
    public const int HeaderSize = 0x12;

    [FieldOffset(0)]
    public ushort magic;
    [FieldOffset(2)]
    public SacredMsgType type1;
    [FieldOffset(4)]
    public SacredMsgType type2;
    [FieldOffset(6)]
    public uint unknown1;
    [FieldOffset(10)]
    public int dataLength;
    [FieldOffset(14)]
    public uint unknown2;

    [FieldOffset(0)]
    public fixed byte rawData[HeaderSize];

    public Span<byte> AsSpan()
    {
        fixed (byte* p = rawData)
            return new(p, HeaderSize);
    }
}
