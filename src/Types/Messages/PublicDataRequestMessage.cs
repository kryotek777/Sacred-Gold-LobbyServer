using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record PublicDataRequestMessage(
    int PermId,
    short BlockId,
    int Offset,
    int Length
) : ISerializable<PublicDataRequestMessage>
{
    public PublicDataRequestMessage(PublicDataRequestMessageData data)
        : this(
            data.PermId,
            data.BlockId,
            data.Offset,
            data.Length
        )
    { }

    public static PublicDataRequestMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out PublicDataRequestMessageData data);
        return new PublicDataRequestMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public PublicDataRequestMessageData ToStruct()
    {
        PublicDataRequestMessageData result = new();
        result.PermId = PermId;
        result.BlockId = BlockId;
        result.Offset = Offset;
        result.Length = Length;
        return result;
    }
}
