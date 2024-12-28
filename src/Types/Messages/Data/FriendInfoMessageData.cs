using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct FriendInfoMessageData
{

    public int DestinationPermId;

    public BuddyStatus Status;

    public fixed byte Username[Constants.NameMaxLength];

    public int PermId;

    public int GameId;

    public fixed byte GameName[Constants.NameMaxLength];

    public int ChannelId;

    public fixed byte ChannelName[Constants.NameMaxLength];
}
