using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record ImportantMessage(
    bool VeryImportant,
    string MessageData,
    int DestinationPermId
) : ISerializable<ImportantMessage>
{
    public ImportantMessage(ImportantMessageData data)
        : this(
            data.VeryImportant,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.MessageData, Constants.ChatMaxLength)),
            data.DestinationPermId
        )
    { }

    public static ImportantMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ImportantMessageData data);
        return new ImportantMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ImportantMessageData ToStruct()
    {
        ImportantMessageData result = new();
        result.VeryImportant = VeryImportant;
        Utils.SerializeString(MessageData, new Span<byte>(result.MessageData, Constants.ChatMaxLength));
        result.DestinationPermId = DestinationPermId;
        return result;
    }
}
