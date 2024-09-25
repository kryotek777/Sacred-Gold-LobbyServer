using System.Runtime.InteropServices;
using System.Text;
using Sacred.Networking.Structs;

namespace Sacred.Networking.Types;

public record LogOn(
    uint Magic,
    uint ConnectionId,
    string Username,
    string Password,
    int Unknown
) : ISerializable<LogOn>
{
    public const uint LogOnMagic = 0xDABAFBEF;
    public const uint LogOnConnId = 0xEFFFFFEE;

    public LogOn(uint connId) : this(LogOnMagic, connId, "user", "-", 0) { }

    public static LogOn Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var Magic = reader.ReadUInt32();
        var ConnectionId = reader.ReadUInt32();
        var Username = Utils.Win1252ToString(reader.ReadBytes(32));
        var Password = Utils.Win1252ToString(reader.ReadBytes(8));
        var Unknown = reader.ReadInt32();

        return new LogOn(Magic, ConnectionId, Username, Password, Unknown);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Magic);
        writer.Write(ConnectionId);
        writer.Write(Utils.StringToWin1252(Username).PadToSize(32));
        writer.Write(Utils.StringToWin1252(Password).PadToSize(8));
        writer.Write(Unknown);

        return ms.ToArray();
    }
}