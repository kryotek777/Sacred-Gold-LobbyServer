using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record PublicInfoMessage(
    int PermId,
    string Username
) : ISerializable<PublicInfoMessage>
{
    public PublicInfoMessage(PublicInfoMessageData data)
        : this(
            data.PermId,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Username, Constants.NameMaxLength))
        )
    { }

    public static PublicInfoMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out PublicInfoMessageData data);
        return new PublicInfoMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public PublicInfoMessageData ToStruct()
    {
        PublicInfoMessageData result = new();
        result.PermId = PermId;
        Utils.SerializeString(Username, new Span<byte>(result.Username, Constants.NameMaxLength));
        return result;
    }
}
