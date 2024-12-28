using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record FindUserMessage(
    string Name
) : ISerializable<FindUserMessage>
{
    public FindUserMessage(FindUserMessageData data)
        : this(
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Name, Constants.NameMaxLength))
        )
    { }

    public static FindUserMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out FindUserMessageData data);
        return new FindUserMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public FindUserMessageData ToStruct()
    {
        FindUserMessageData result = new();
        Utils.SerializeString(Name, new Span<byte>(result.Name, Constants.NameMaxLength));
        return result;
    }
}
