using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record FriendInfoMessage(
    int DestinationPermId,
    BuddyStatus Status,
    string Username,
    int PermId,
    int GameId,
    string GameName,
    int ChannelId,
    string ChannelName
) : ISerializable<FriendInfoMessage>
{
    public FriendInfoMessage(FriendInfoMessageData data)
        : this(
            data.DestinationPermId,
            data.Status,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Username, Constants.NameMaxLength)),
            data.PermId,
            data.GameId,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.GameName, Constants.NameMaxLength)),
            data.ChannelId,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.ChannelName, Constants.NameMaxLength))
        )
    { }

    public static FriendInfoMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out FriendInfoMessageData data);
        return new FriendInfoMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }


    public FriendInfoMessageData ToStruct()
    {
        FriendInfoMessageData result = new();
        result.DestinationPermId = DestinationPermId;
        result.Status = Status;
        Utils.SerializeString(Username, new Span<byte>(result.Username, Constants.NameMaxLength));
        result.PermId = PermId;
        result.GameId = GameId;
        Utils.SerializeString(GameName, new Span<byte>(result.GameName, Constants.NameMaxLength));
        result.ChannelId = ChannelId;
        Utils.SerializeString(ChannelName, new Span<byte>(result.ChannelName, Constants.NameMaxLength));
        return result;
    }
}
