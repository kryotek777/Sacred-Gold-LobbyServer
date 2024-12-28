using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record ChatMessage(
    string SenderName,
    int SenderPermId,
    int DestinationPermId,
    string Text
) : ISerializable<ChatMessage>
{
    public ChatMessage(ChatMessageData data)
        : this(
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.SenderName, Constants.NameMaxLength)),
            data.SenderPermId,
            data.DestinationPermId,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Text, Constants.ChatMaxLength))
        )
    { }

    public static ChatMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ChatMessageData data);
        return new ChatMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ChatMessageData ToStruct()
    {
        ChatMessageData result = new();
        Utils.SerializeString(SenderName, new Span<byte>(result.SenderName, Constants.NameMaxLength));
        result.SenderPermId = SenderPermId;
        result.DestinationPermId = DestinationPermId;
        Utils.SerializeString(Text, new Span<byte>(result.Text, Constants.ChatMaxLength));
        return result;
    }
}
