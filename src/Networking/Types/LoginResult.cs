
using System.Net;

namespace Sacred.Networking.Types;

/// <summary>
/// <para> The lobbyserver answers a <see cref="LoginRequest"/> </para>
/// <para> Message: <see cref="SacredMsgType.ClientLoginResult"/> (2) </para>
/// <param name="Result">The result code for the login request</param>
/// <param name="Ip">The public ip address of the client</param>
/// <param name="PermId">The client's permId</param>
/// <param name="Message">A message that's shown in sacred's network log</param>
/// </summary>
public record LoginResult(
    LobbyResults Result,
    IPAddress Ip,
    int PermId,
    string Message
) : ISerializable<LoginResult>
{
    public static LoginResult Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var result = (LobbyResults)reader.ReadInt32();
        var ip = new IPAddress(reader.ReadBytes(4));
        var permId = reader.ReadInt32();
        var message = Utils.Win1252ToString(reader.ReadBytes(256));

        return new LoginResult(result, ip, permId, message);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((int)Result);
        writer.Write(Ip.GetAddressBytes());
        writer.Write(PermId);
        writer.Write(Utils.StringToWin1252(Message, 256));

        return ms.ToArray();
    }
}