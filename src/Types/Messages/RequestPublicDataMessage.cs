using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record RequestPublicDataMessage(
    int PermId,
    short BlockId,
    int Offset,
    int Length
) : ISerializable<RequestPublicDataMessage>
{
    public RequestPublicDataMessage(RequestPublicDataMessageData data)
        : this(
            data.PermId,
            data.BlockId,
            data.Offset,
            data.Length
        )
    { }

    public static RequestPublicDataMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out RequestPublicDataMessageData data);
        return new RequestPublicDataMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public RequestPublicDataMessageData ToStruct()
    {
        return new RequestPublicDataMessageData
        {
            PermId = PermId,
            BlockId = BlockId,
            Offset = Offset,
            Length = Length
        };
    }
}
