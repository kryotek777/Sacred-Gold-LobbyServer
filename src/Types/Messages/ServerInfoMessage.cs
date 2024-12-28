using System.Net;
using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record ServerInfoMessage(
    string Name,
    IPAddress LocalIp,
    IPAddress ExternalIp,
    uint Port,
    short PlayerCount,
    short MaxPlayers,
    ServerFlags Flags,
    uint ServerId,
    int NetworkVersion,
    int ClientGameVersion,
    int ChannelId
) : ISerializable<ServerInfoMessage>
{
    public ServerInfoMessage(ServerInfoMessageData data)
        : this(
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Name, Constants.NameMaxLength)),
            new IPAddress(new ReadOnlySpan<byte>(data.LanIp, 4)),
            new IPAddress(new ReadOnlySpan<byte>(data.ExternalIp, 4)),
            data.Port,
            data.PlayerCount,
            data.PlayerMax,
            (ServerFlags)data.Flags,
            data.ServerId,
            data.NetworkVersion,
            data.ClientGameVersion,
            data.ChannelId
        )
    { }

    public static ServerInfoMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ServerInfoMessageData data);
        return new ServerInfoMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ServerInfoMessageData ToStruct()
    {
        ServerInfoMessageData result = new();
        Utils.SerializeString(Name, new Span<byte>(result.Name, Constants.NameMaxLength));
        LocalIp.GetAddressBytes().CopyTo(new Span<byte>(result.LanIp, 4));
        ExternalIp.GetAddressBytes().CopyTo(new Span<byte>(result.ExternalIp, 4));
        result.Port = Port;
        result.PlayerCount = PlayerCount;
        result.PlayerMax = MaxPlayers;
        result.Flags = (int)Flags;
        result.ServerId = ServerId;
        result.NetworkVersion = NetworkVersion;
        result.ClientGameVersion = ClientGameVersion;
        result.ChannelId = ChannelId;
        return result;
    }
}
