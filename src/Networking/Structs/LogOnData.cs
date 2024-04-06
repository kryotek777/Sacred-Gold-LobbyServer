using System.Runtime.InteropServices;

namespace Sacred.Networking.Structs;

[StructLayout(LayoutKind.Explicit, Size = DataSize)]
public unsafe struct LogOnData
{
    public const int DataSize = 0x34;

    [FieldOffset(0)]
    public uint magic;

    [FieldOffset(4)]
    public uint connId;

    [FieldOffset(8)]
    public fixed byte user[32];

    [FieldOffset(40)]
    public fixed byte password[8];

    [FieldOffset(48)]
    public uint unknown;

    [FieldOffset(0)]
    public fixed byte rawData[DataSize];
}
