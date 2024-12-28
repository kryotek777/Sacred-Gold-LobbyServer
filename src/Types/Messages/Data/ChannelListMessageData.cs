using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ChannelListMessageData
{
    public ushort Count;

    public fixed byte Channels[Constants.ChannelMax * 91];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ChannelInfoData
{
    public fixed byte Name[Constants.NameMaxLength];
    public bool AuthorizedOnly;
    public uint Flags;
    public ushort Id;
    public ushort UserCount;
    public ushort GameCount;
}