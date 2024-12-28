using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record JoinChannelMessage(
    ushort ChannelId
) : ISerializable<JoinChannelMessage>
{
    public JoinChannelMessage(JoinChannelMessageData data)
        : this(data.ChannelId) { }

    public static JoinChannelMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out JoinChannelMessageData data);
        return new JoinChannelMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public JoinChannelMessageData ToStruct()
    {
        return new JoinChannelMessageData
        {
            ChannelId = ChannelId
        };
    }
}
