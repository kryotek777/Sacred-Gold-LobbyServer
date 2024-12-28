using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record RequestServerListMessage(
    int ChannelId
) : ISerializable<RequestServerListMessage>
{
    public RequestServerListMessage(RequestServerListMessageData data)
        : this(data.ChannelId) { }

    public static RequestServerListMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out RequestServerListMessageData data);
        return new RequestServerListMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public RequestServerListMessageData ToStruct()
    {
        return new RequestServerListMessageData
        {
            ChannelId = ChannelId
        };
    }
}
