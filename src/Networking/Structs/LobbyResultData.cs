using System.Runtime.InteropServices;

namespace Sacred.Networking.Structs;

[StructLayout(LayoutKind.Explicit, Size = DataSize)]
public unsafe struct LobbyResultData
{
    public const int DataSize = 0x8;

    [FieldOffset(0)]
    public int result;

    [FieldOffset(4)]
    public int last;

    [FieldOffset(0)]
    public fixed byte rawData[DataSize];
}
