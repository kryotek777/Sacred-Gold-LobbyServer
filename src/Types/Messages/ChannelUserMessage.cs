using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record ChannelUserMessage(
    int PermId,
    string Username
) : ISerializable<ChannelUserMessage>
{
    public ChannelUserMessage(ChannelUserMessageData data)
        : this(
            data.PermId,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Username, Constants.NameMaxLength))
        )
    { }

    public static ChannelUserMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ChannelUserMessageData data);
        return new ChannelUserMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ChannelUserMessageData ToStruct()
    {
        ChannelUserMessageData result = new();
        result.PermId = PermId;
        Utils.SerializeString(Username, new Span<byte>(result.Username, Constants.NameMaxLength));
        return result;
    }
}
