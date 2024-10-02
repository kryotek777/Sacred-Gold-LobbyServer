using System.Text;

namespace Sacred.Networking.Types;

public record SacredChatMessage
(
    string SenderName,
    int SenderPermId,
    int DestinationPermId,
    string Message
) : ISerializable<SacredChatMessage>
{

    public static SacredChatMessage Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var SenderName = Utils.Win1252ToString(reader.ReadBytes(Constants.UsernameMaxLength));
        var SenderPermId = reader.ReadInt32();
        var DestinationPermId = reader.ReadInt32();
        var Message = Utils.Win1252ToString(reader.ReadBytes(Constants.ChatMessageMaxLength));

        return new SacredChatMessage(SenderName, SenderPermId, DestinationPermId, Message);      
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Utils.StringToWin1252(SenderName, Constants.UsernameMaxLength));
        writer.Write(SenderPermId);
        writer.Write(DestinationPermId);
        writer.Write(Utils.StringToWin1252(Message, Constants.ChatMessageMaxLength));

        return ms.ToArray();
    }
}