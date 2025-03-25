namespace Lobby.Types;

public record LogOn(
    uint Magic,
    uint ConnectionId,
    string Username,
    string Password,
    int Unknown
)
{
    public const uint LogOnMagic = 0xDABAFBEF;
    public const uint LogOnConnId = 0xEFFFFFEE;

    public LogOn(uint connId) : this(LogOnMagic, connId, "user", "-", 0) { }

    public static LogOn Deserialize(ReadOnlySpan<byte> data)
    {
        var reader = new SpanReader(data);
        var Magic = reader.ReadUInt32();
        var ConnectionId = reader.ReadUInt32();
        var Username = Utils.Win1252ToString(reader.ReadBytes(32));
        var Password = Utils.Win1252ToString(reader.ReadBytes(8));
        var Unknown = reader.ReadInt32();

        return new LogOn(Magic, ConnectionId, Username, Password, Unknown);
    }

    public void Serialize(SpanWriter writer)
    {
        writer.Write(Magic);
        writer.Write(ConnectionId);
        writer.Write(Utils.StringToWin1252(Username, 32));
        writer.Write(Utils.StringToWin1252(Password, 8));
        writer.Write(Unknown);
    }
}