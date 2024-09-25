namespace Sacred.Networking.Types;

public record ServerListRequest(
    int ChannelId
) : ISerializable<ServerListRequest>
{
    public static ServerListRequest Deserialize(ReadOnlySpan<byte> span)
    {
        var channel = BitConverter.ToInt32(span);

        return new ServerListRequest(channel);
    }

    public byte[] Serialize()
    {
        return BitConverter.GetBytes(ChannelId);
    }
}