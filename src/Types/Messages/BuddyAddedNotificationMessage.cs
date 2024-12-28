using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record BuddyAddedNotificationMessage(
    int DestinationPermId,
    int AdderPermId,
    string AdderName
) : ISerializable<BuddyAddedNotificationMessage>
{
    public BuddyAddedNotificationMessage(BuddyAddedNotificationMessageData data)
        : this(
            data.DestinationPermId,
            data.AdderPermId,
            Utils.DeserializeString(new ReadOnlySpan<byte>(data.AdderName, Constants.NameMaxLength))
        )
    { }

    public static BuddyAddedNotificationMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out BuddyAddedNotificationMessageData data);
        return new BuddyAddedNotificationMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public BuddyAddedNotificationMessageData ToStruct()
    {
        BuddyAddedNotificationMessageData result = new();
        result.DestinationPermId = DestinationPermId;
        result.AdderPermId = AdderPermId;
        Utils.SerializeString(AdderName, new Span<byte>(result.AdderName, Constants.NameMaxLength));
        return result;
    }
}
