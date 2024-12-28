using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record PublicInfoRequestMessage(
    int PermId
) : ISerializable<PublicInfoRequestMessage>
{
    public PublicInfoRequestMessage(PublicInfoRequestMessageData data)
        : this(data.PermId) { }

    public static PublicInfoRequestMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out PublicInfoRequestMessageData data);
        return new PublicInfoRequestMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public PublicInfoRequestMessageData ToStruct()
    {
        return new PublicInfoRequestMessageData
        {
            PermId = PermId
        };
    }
}
