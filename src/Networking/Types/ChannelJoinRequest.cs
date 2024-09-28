namespace Sacred.Networking.Types;

public record ChannelJoinRequest(
    short ChannelId
) : ISerializable<ChannelJoinRequest>
{
    public static ChannelJoinRequest Deserialize(ReadOnlySpan<byte> span)
    {
        var channel = BitConverter.ToInt16(span);

        return new ChannelJoinRequest(channel);
    }

    public byte[] Serialize()
    {
        return BitConverter.GetBytes(ChannelId);
    }
}