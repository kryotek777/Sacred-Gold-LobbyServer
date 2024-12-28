using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BuddyMessageData
{
    public int FromPermId;
    public int BuddyPermId;
}
