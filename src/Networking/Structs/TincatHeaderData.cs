using System.Runtime.InteropServices;

namespace Sacred.Networking.Structs;

[StructLayout(LayoutKind.Explicit, Size = DataSize)]
public unsafe struct TincatHeaderData
{
    public const int DataSize = 0x1C; //28

    [FieldOffset(0)]
    public uint magic;

    [FieldOffset(4)]
    public uint from;

    [FieldOffset(8)]
    public uint to;

    [FieldOffset(12)]
    public ushort msgType;

    [FieldOffset(16)]
    public uint unknown;

    [FieldOffset(20)]
    public int payloadLength;

    [FieldOffset(24)]
    public uint checksum;

    [FieldOffset(0)]
    public fixed byte rawData[DataSize];
}