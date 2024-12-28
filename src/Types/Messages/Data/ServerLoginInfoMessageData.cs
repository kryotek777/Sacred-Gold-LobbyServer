using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ServerLoginInfoMessageData
{
    public fixed byte Ip[4];
}
