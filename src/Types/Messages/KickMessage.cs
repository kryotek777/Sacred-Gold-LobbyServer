using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record KickMessage(
    string Reason
) : ISerializable<KickMessage>
{
    public KickMessage(KickMessageData data)
        : this(Utils.DeserializeString(new ReadOnlySpan<byte>(data.Reason, Constants.ChatMaxLength))) { }

    public static KickMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out KickMessageData data);
        return new KickMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public KickMessageData ToStruct()
    {
        KickMessageData result = new();
        Utils.SerializeString(Reason, new Span<byte>(result.Reason, Constants.ChatMaxLength));
        return result;
    }
}
