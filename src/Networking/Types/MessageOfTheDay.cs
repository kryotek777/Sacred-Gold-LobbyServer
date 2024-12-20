namespace Lobby.Networking.Types;

public record MessageOfTheDay(ushort Id, string Message) : ISerializable<MessageOfTheDay>
{
    public static MessageOfTheDay Deserialize(ReadOnlySpan<byte> span)
    {
        var reader = new SpanReader(span);

        var id = reader.ReadUInt16();
        var length = reader.ReadUInt16();
        reader.Position += 128;
        var message = Utils.Win1252ToString(reader.ReadBytes(length));

        return new MessageOfTheDay(id, message);
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();

        stream.Write(Id);
        stream.Write((ushort)Message.Length);
        stream.Position += 128;
        stream.Write(Utils.StringToWin1252(Message));

        return stream.ToArray();
    }
}