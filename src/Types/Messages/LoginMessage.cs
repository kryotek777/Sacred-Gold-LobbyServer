using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record LoginMessage(
    string Username,
    string Password,
    string CdKey,
    uint PatchLevel,
    uint ProgramVersion,
    string CdKey2
) : ISerializable<LoginMessage>
{
    public LoginMessage(LoginMessageData data)
        : this(
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Username, Constants.NameMaxLength)),
            Utils.TincatDecrypt(new ReadOnlySpan<byte>(data.Password, Constants.PasswordMaxLength)),
            Utils.TincatDecrypt(new ReadOnlySpan<byte>(data.CdKey, Constants.KeyMaxLength)),
            data.PatchLevel,
            data.ProgramVersion,
            Utils.TincatDecrypt(new ReadOnlySpan<byte>(data.CdKey2, Constants.KeyMaxLength))
        )
    { }

    public static LoginMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out LoginMessageData data);
        return new LoginMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public LoginMessageData ToStruct()
    {
        LoginMessageData result = new();
        Utils.SerializeString(Username, new Span<byte>(result.Username, Constants.NameMaxLength));
        Utils.TincatEncrypt(Password, new Span<byte>(result.Password, Constants.PasswordMaxLength));
        Utils.TincatEncrypt(CdKey, new Span<byte>(result.CdKey, Constants.KeyMaxLength));
        result.PatchLevel = PatchLevel;
        result.ProgramVersion = ProgramVersion;
        Utils.TincatEncrypt(CdKey2, new Span<byte>(result.CdKey2, Constants.KeyMaxLength));
        return result;
    }
}
