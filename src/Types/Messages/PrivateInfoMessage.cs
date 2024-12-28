using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record PrivateInfoMessage(
    string Password,
    string Mail
) : ISerializable<PrivateInfoMessage>
{
    public PrivateInfoMessage(PrivateInfoMessageData data)
        : this(
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Password, Constants.PasswordMaxLength)),
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Mail, Constants.MailMaxLength))
        )
    { }

    public static PrivateInfoMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out PrivateInfoMessageData data);
        return new PrivateInfoMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public PrivateInfoMessageData ToStruct()
    {
        PrivateInfoMessageData result = new();
        Utils.SerializeString(Password, new Span<byte>(result.Password, Constants.PasswordMaxLength));
        Utils.SerializeString(Mail, new Span<byte>(result.Mail, Constants.MailMaxLength));
        return result;
    }
}
