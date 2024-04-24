using System.Runtime.InteropServices;

namespace Sacred.Networking.Structs;

[StructLayout(LayoutKind.Explicit, Size = DataSize)]
public unsafe struct ServerInfoData
{
    public const int DataSize = 0x74;

    [FieldOffset(0)]
    public fixed byte name[80];
    [FieldOffset(84)]
    public int ipAddress;
    [FieldOffset(88)]
    public int port;
    [FieldOffset(92)]
    public short currentPlayers;
    [FieldOffset(94)]
    public short maxPlayers;
    [FieldOffset(96)]
    public int flags;
    [FieldOffset(100)]
    public uint serverId;
    [FieldOffset(108)]
    public int version;
    [FieldOffset(112)]
    public int hidden;

    [FieldOffset(0)]
    public fixed byte rawData[DataSize];
}
