using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record MotdRequestMessage(
    ushort Id,
    byte[] Reserved
) : ISerializable<MotdRequestMessage>
{
    public MotdRequestMessage(MotdRequestMessageData data)
        : this(
            data.Id,
            new ReadOnlySpan<byte>(data.Reserved, 128).ToArray()
        )
    { }

    public static MotdRequestMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out MotdRequestMessageData data);
        return new MotdRequestMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public MotdRequestMessageData ToStruct()
    {
        MotdRequestMessageData result = new();
        result.Id = Id;
        Reserved.CopyTo(new Span<byte>(result.Reserved, 128));
        return result;
    }
}
