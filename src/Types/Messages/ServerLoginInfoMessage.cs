using System.Net;
using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record ServerLoginInfoMessage(
    IPAddress Ip
) : ISerializable<ServerLoginInfoMessage>
{
    public ServerLoginInfoMessage(ServerLoginInfoMessageData data)
        : this(new IPAddress(new ReadOnlySpan<byte>(data.Ip, 4))) { }

    public static ServerLoginInfoMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ServerLoginInfoMessageData data);
        return new ServerLoginInfoMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ServerLoginInfoMessageData ToStruct()
    {
        ServerLoginInfoMessageData result = new();
        Ip.GetAddressBytes().CopyTo(new Span<byte>(result.Ip, 4));
        return result;
    }
}
