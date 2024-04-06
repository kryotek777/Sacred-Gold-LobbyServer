using System.Runtime.InteropServices;

namespace Sacred.Networking.Structs;

[StructLayout(LayoutKind.Explicit, Size = DataSize)]
public unsafe struct SacredHeaderData
{
    public const int DataSize = 0x12;

    [FieldOffset(0)]
    public ushort magic;

    [FieldOffset(2)]
    public ushort type1;

    [FieldOffset(4)]
    public ushort type2;

    [FieldOffset(6)]
    public uint unknown1;

    [FieldOffset(10)]
    public int payloadLength;
    
    [FieldOffset(14)]
    public uint unknown2;

    [FieldOffset(0)]
    public fixed byte rawData[DataSize];
}
