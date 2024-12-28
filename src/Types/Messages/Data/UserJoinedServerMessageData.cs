using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UserJoinedServerMessageData
{
    public int PermId;
    public short BlockId;
    public ushort Check;
}
