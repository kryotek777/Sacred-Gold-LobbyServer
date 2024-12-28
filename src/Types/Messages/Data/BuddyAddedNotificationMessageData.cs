using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BuddyAddedNotificationMessageData
{
    public int DestinationPermId;
    public int AdderPermId;
    public fixed byte AdderName[Constants.NameMaxLength];
}
