using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record RegistrationMessage(
    string Username,
    string Password,
    string CdKey,
    string Mail,
    uint PatchLevel,
    uint ProgramVersion,
    string CdKey2
) : ISerializable<RegistrationMessage>
{
    public RegistrationMessage(RegistrationMessageData data)
        : this(
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Username, Constants.NameMaxLength)),
            Utils.TincatDecrypt(new ReadOnlySpan<byte>(data.Password, Constants.PasswordMaxLength)),
            Utils.TincatDecrypt(new ReadOnlySpan<byte>(data.CdKey, Constants.KeyMaxLength)),
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Mail, Constants.MailMaxLength)),
            data.PatchLevel,
            data.ProgramVersion,
            Utils.TincatDecrypt(new ReadOnlySpan<byte>(data.CdKey2, Constants.KeyMaxLength))
        )
    { }

    public static RegistrationMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out RegistrationMessageData data);
        return new RegistrationMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public RegistrationMessageData ToStruct()
    {
        RegistrationMessageData result = new();
        Utils.SerializeString(Username, new Span<byte>(result.Username, Constants.NameMaxLength));
        Utils.TincatEncrypt(Password, new Span<byte>(result.Password, Constants.PasswordMaxLength));
        Utils.TincatEncrypt(CdKey, new Span<byte>(result.CdKey, Constants.KeyMaxLength));
        Utils.SerializeString(Mail, new Span<byte>(result.Mail, Constants.MailMaxLength));
        result.PatchLevel = PatchLevel;
        result.ProgramVersion = ProgramVersion;
        Utils.TincatEncrypt(CdKey2, new Span<byte>(result.CdKey2, Constants.KeyMaxLength));
        return result;
    }
}
