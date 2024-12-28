using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record UserJoinedServerMessage(
    int PermId,
    short BlockId,
    ushort Check
) : ISerializable<UserJoinedServerMessage>
{
    public UserJoinedServerMessage(UserJoinedServerMessageData data)
        : this(data.PermId, data.BlockId, data.Check) { }

    public static UserJoinedServerMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out UserJoinedServerMessageData data);
        return new UserJoinedServerMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public UserJoinedServerMessageData ToStruct()
    {
        return new UserJoinedServerMessageData
        {
            PermId = PermId,
            BlockId = BlockId,
            Check = Check
        };
    }
}
