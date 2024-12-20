namespace Lobby.Networking.Types;

public record ImportantMessage(bool ShowPopup, string Message, int DestinationPermId) : ISerializable<ImportantMessage>
{
    public static ImportantMessage Deserialize(ReadOnlySpan<byte> span)
    {
        var reader = new SpanReader(span);

        var ShowPopup = reader.ReadBoolean();
        var Message = Utils.Win1252ToString(reader.ReadBytes(Constants.ChatMessageMaxLength));
        var DestinationPermId = reader.ReadInt32();

        return new ImportantMessage(ShowPopup, Message, DestinationPermId);
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();

        stream.Write(ShowPopup);
        stream.Write(Utils.StringToWin1252(Message, Constants.ChatMessageMaxLength));
        stream.Write(DestinationPermId);

        return stream.ToArray();
    }
}