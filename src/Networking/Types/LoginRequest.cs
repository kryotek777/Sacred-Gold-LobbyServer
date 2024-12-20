namespace Lobby.Networking.Types;

/// <summary>
/// <para> The client requested to log into the lobbyserver </para>
/// <para> Message: <see cref="SacredMsgType.ClientLoginRequest"/> (2) </para>
/// <para> Answer: <see cref="LoginResult"/> </para>
/// </summary>
/// <param name="Username">The client's username</param>
/// <param name="Password">The client's password</param>
/// <param name="CdKey1">The client's game key</param>
/// <param name="CdKey2">The client's underworld expansion key</param>
/// <param name="NetworkProtocolVersion">The version of the client's network protocol</param>
/// <param name="ClientGameVersion">The version of the client's game</param>
public record LoginRequest(
    string Username,
    string Password,
    string CdKey1,
    string CdKey2,
    uint NetworkProtocolVersion,
    uint ClientGameVersion
) : ISerializable<LoginRequest>
{
    public static LoginRequest Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var user = Utils.Win1252ToString(reader.ReadBytes(Constants.UsernameMaxLength));
        var pass = Utils.TincatDecrypt(reader.ReadBytes(Constants.PasswordMaxLength));
        var cd1 = Utils.TincatDecrypt(reader.ReadBytes(Constants.CdKeyLength).AsSpan(0..20));
        var netVer = reader.ReadUInt32();
        var clVer = reader.ReadUInt32();
        var cd2 = Utils.TincatDecrypt(reader.ReadBytes(Constants.CdKeyLength).AsSpan(0..20));

        return new LoginRequest(user, pass, cd1, cd2, netVer, clVer);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Utils.StringToWin1252(Username, Constants.UsernameMaxLength));
        writer.Write(Utils.TincatEncrypt(Password, Constants.PasswordMaxLength));
        writer.Write(Utils.TincatEncrypt(CdKey1, Constants.CdKeyLength));
        writer.Write((byte)0); //Pad CDKeys to 21 bytes
        writer.Write(NetworkProtocolVersion);
        writer.Write(ClientGameVersion);
        writer.Write(Utils.TincatEncrypt(CdKey2, Constants.CdKeyLength));
        writer.Write((byte)0); //Pad CDKeys to 21 bytes

        return ms.ToArray();
    }
}