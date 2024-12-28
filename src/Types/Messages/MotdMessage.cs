using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record MotdMessage(
    ushort Id,
    ushort Length,
    byte[] Reserved,
    string Text
) : ISerializable<MotdMessage>
{
    public MotdMessage(ushort Id, string Text) : this(Id, (ushort)Text.Length, Array.Empty<byte>(), Text) { }

    public MotdMessage(MotdMessageData data)
        : this(
            data.Id,
            data.Length,
            new ReadOnlySpan<byte>(data.Reserved, 128).ToArray(),
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Text, Constants.MotdMaxLength))
        )
    { }

    public MotdMessageData ToStruct()
    {
        MotdMessageData result = new();
        result.Id = Id;
        result.Length = Length;
        Reserved.CopyTo(new Span<byte>(result.Reserved, 128));
        Utils.SerializeString(Text, new Span<byte>(result.Text, Constants.MotdMaxLength));
        return result;
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public static MotdMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out MotdMessageData data);
        return new MotdMessage(data);
    }
}