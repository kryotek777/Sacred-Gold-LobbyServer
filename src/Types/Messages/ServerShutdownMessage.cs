using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record ServerShutdownMessage(
    string MessageData
) : ISerializable<ServerShutdownMessage>
{
    public ServerShutdownMessage(ServerShutdownMessageData data)
        : this(Utils.DeserializeString(new ReadOnlySpan<byte>(data.MessageData, Constants.ChatMaxLength))) { }

    public static ServerShutdownMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ServerShutdownMessageData data);
        return new ServerShutdownMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ServerShutdownMessageData ToStruct()
    {
        ServerShutdownMessageData result = new();
        Utils.SerializeString(MessageData, new Span<byte>(result.MessageData, Constants.ChatMaxLength));
        return result;
    }
}
