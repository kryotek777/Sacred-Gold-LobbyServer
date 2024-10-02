using System.Net;

namespace Sacred.Networking.Types;

/// <summary>
/// Generic answer to a message
/// </summary>
/// <param name="Result">The result code</param>
/// <param name="AnsweringTo">The message code we're answering to</param>
public record ServerInfo(
    string Name,
    IPAddress LocalIp,
    IPAddress ExternalIp,
    int Port,
    short CurrentPlayers,
    short MaxPlayers,
    int Flags,
    uint ServerId,
    uint NetworkProtocolVersion,
    uint ClientGameVersion,
    int ChannelId
) : ISerializable<ServerInfo>
{
    public static ServerInfo Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var Name = Utils.Win1252ToString(reader.ReadBytes(Constants.UsernameMaxLength));
        var LocalIp = new IPAddress(reader.ReadBytes(4));
        var ExternalIp = new IPAddress(reader.ReadBytes(4));
        var Port = reader.ReadInt32();
        var CurrentPlayers = reader.ReadInt16();
        var MaxPlayers = reader.ReadInt16();
        var Flags = reader.ReadInt32();
        var ServerId = reader.ReadUInt32();
        var NetworkProtocolVersion = reader.ReadUInt32();
        var ClientGameVersion = reader.ReadUInt32();
        var ChannelId = reader.ReadInt32();
        
        return new ServerInfo(Name, LocalIp, ExternalIp, Port, CurrentPlayers, MaxPlayers, Flags, ServerId, NetworkProtocolVersion, ClientGameVersion, ChannelId);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var LocalIp = this.LocalIp ?? IPAddress.None;
        var ExternalIp = this.LocalIp ?? IPAddress.None;

        writer.Write(Utils.StringToWin1252(Name).PadToSize(Constants.UsernameMaxLength));
        writer.Write(LocalIp.GetAddressBytes());
        writer.Write(ExternalIp.GetAddressBytes());
        writer.Write(Port);
        writer.Write(CurrentPlayers);
        writer.Write(MaxPlayers);
        writer.Write(Flags);
        writer.Write(ServerId);
        writer.Write(NetworkProtocolVersion);
        writer.Write(ClientGameVersion);
        writer.Write(ChannelId);

        return ms.ToArray();
    }
}
