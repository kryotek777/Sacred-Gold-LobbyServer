using System.Runtime.InteropServices;
using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record ChannelListMessage(
    ushort Count,
    List<ChannelInfo> Channels
) : ISerializable<ChannelListMessage>
{
    public ChannelListMessage(ChannelListMessageData data)
        : this(
            data.Count,
            DeserializeChannels(data)
        )
    { }

    public ChannelListMessage(List<ChannelInfo> channels) : this((ushort)channels.Count, channels) {}

    public static ChannelListMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ChannelListMessageData data);
        return new ChannelListMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ChannelListMessageData ToStruct()
    {
        ChannelListMessageData result = new();
        result.Count = Count;

        unsafe
        {
            var span = new Span<byte>(result.Channels, result.Count * sizeof(ChannelInfoData));
            for (int i = 0; i < Count; i++)
            {
                var data = Channels[i].ToStruct();
                MemoryMarshal.Write(span.Slice(i * sizeof(ChannelInfoData)), data);
            }
        }

        return result;
    }

    private static unsafe List<ChannelInfo> DeserializeChannels(ChannelListMessageData data)
    {
        var channels = new List<ChannelInfo>();

        for (int i = 0; i < data.Count; i++)
        {
            var span = new ReadOnlySpan<byte>(
                data.Channels + i * sizeof(ChannelInfoData),
                sizeof(ChannelInfoData));

            Utils.FromSpan(span, out ChannelInfoData channelInfoData);

            var channelInfo = new ChannelInfo(channelInfoData);

            channels.Add(channelInfo);
        }

        return channels;
    }
}

public unsafe record ChannelInfo(
    string Name,
    bool AuthorizedOnly,
    uint Flags,
    ushort Id,
    ushort UserCount,
    ushort GameCount
)
{
    public ChannelInfo(ChannelInfoData data)
        : this(
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.Name, Constants.NameMaxLength)),
            data.AuthorizedOnly,
            data.Flags,
            data.Id,
            data.UserCount,
            data.GameCount
        )
    { }

    public ChannelInfoData ToStruct()
    {
        ChannelInfoData result = new();
        Utils.SerializeString(Name, new Span<byte>(result.Name, Constants.NameMaxLength));
        result.AuthorizedOnly = AuthorizedOnly;
        result.Flags = Flags;
        result.Id = Id;
        result.UserCount = UserCount;
        result.GameCount = GameCount;
        return result;
    }
}