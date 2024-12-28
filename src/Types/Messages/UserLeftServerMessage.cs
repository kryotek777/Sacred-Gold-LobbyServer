using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record UserLeftServerMessage(
    int PermId
) : ISerializable<UserLeftServerMessage>
{
    public UserLeftServerMessage(UserLeftServerMessageData data)
        : this(data.PermId) { }

    public static UserLeftServerMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out UserLeftServerMessageData data);
        return new UserLeftServerMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public UserLeftServerMessageData ToStruct()
    {
        return new UserLeftServerMessageData
        {
            PermId = PermId
        };
    }
}
