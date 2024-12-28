using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record GameLogoutMessage() : ISerializable<GameLogoutMessage>
{
    public GameLogoutMessage(GameLogoutMessageData data) : this() { }

    public static GameLogoutMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out GameLogoutMessageData data);
        return new GameLogoutMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public GameLogoutMessageData ToStruct() => new();
}
