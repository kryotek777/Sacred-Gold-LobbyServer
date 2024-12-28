using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public record ClosedNetNewCharacterMessage(
    short BlockId,
    short TemplateId
) : ISerializable<ClosedNetNewCharacterMessage>
{
    public ClosedNetNewCharacterMessage(ClosedNetNewCharacterMessageData data)
        : this(data.BlockId, data.TemplateId) { }

    public static ClosedNetNewCharacterMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ClosedNetNewCharacterMessageData data);
        return new ClosedNetNewCharacterMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ClosedNetNewCharacterMessageData ToStruct()
    {
        return new ClosedNetNewCharacterMessageData
        {
            BlockId = BlockId,
            TemplateId = TemplateId
        };
    }
}
