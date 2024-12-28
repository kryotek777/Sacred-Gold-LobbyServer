using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record BuddyMessage(
    int FromPermId,
    int BuddyPermId
) : ISerializable<BuddyMessage>
{
    public BuddyMessage(BuddyMessageData data)
        : this(data.FromPermId, data.BuddyPermId) { }

    public static BuddyMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out BuddyMessageData data);
        return new BuddyMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public BuddyMessageData ToStruct()
    {
        return new BuddyMessageData
        {
            FromPermId = FromPermId,
            BuddyPermId = BuddyPermId
        };
    }
}
