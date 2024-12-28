using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record SelectPublicDataSetMessage(
    ushort BlockId
) : ISerializable<SelectPublicDataSetMessage>
{
    public SelectPublicDataSetMessage(SelectPublicDataSetMessageData data)
        : this(data.BlockId) { }

    public static SelectPublicDataSetMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out SelectPublicDataSetMessageData data);
        return new SelectPublicDataSetMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public SelectPublicDataSetMessageData ToStruct()
    {
        return new SelectPublicDataSetMessageData
        {
            BlockId = BlockId
        };
    }
}
