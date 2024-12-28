using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record UserKickedFromServerMessage(
    int PermId,
    KickReasons Reason,
    string Text
) : ISerializable<UserKickedFromServerMessage>
{
    public UserKickedFromServerMessage(UserKickedFromServerMessageData data)
        : this(
            data.PermId,
            data.Reason,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Text, Constants.ChatMaxLength))
        )
    { }

    public static UserKickedFromServerMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out UserKickedFromServerMessageData data);
        return new UserKickedFromServerMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public UserKickedFromServerMessageData ToStruct()
    {
        UserKickedFromServerMessageData result = new();
        result.PermId = PermId;
        result.Reason = Reason;
        Utils.SerializeString(Text, new Span<byte>(result.Text, Constants.ChatMaxLength));
        return result;
    }
}
