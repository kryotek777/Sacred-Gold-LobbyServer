using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ServerInfoMessageData
{
    public fixed byte Name[Constants.NameMaxLength];
    public fixed byte LanIp[4];
    public fixed byte ExternalIp[4];
    public uint Port;
    public short PlayerCount;
    public short PlayerMax;
    public int Flags;
    public uint ServerId;
    public int NetworkVersion;
    public int ClientGameVersion;
    public int ChannelId;
}
