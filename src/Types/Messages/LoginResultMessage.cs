using System.Net;
using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record LoginResultMessage(
    LobbyResults Result,
    IPAddress Ip,
    int PermId,
    string Message
) : ISerializable<LoginResultMessage>
{
    public LoginResultMessage(LoginResultMessageData data)
        : this(
            (LobbyResults)data.Result,
            new IPAddress(new ReadOnlySpan<byte>(data.Ip, 4)),
            data.PermId,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Message, Constants.ChatMaxLength))
        )
    { }

    public static LoginResultMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out LoginResultMessageData data);
        return new LoginResultMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public LoginResultMessageData ToStruct()
    {
        LoginResultMessageData result = new();
        result.Result = (int)Result;
        Ip.GetAddressBytes().CopyTo(new Span<byte>(result.Ip, 4));
        result.PermId = PermId;
        Utils.SerializeString(Message, new Span<byte>(result.Message, Constants.ChatMaxLength));
        return result;
    }
}
