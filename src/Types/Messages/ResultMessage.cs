using Lobby.Types;
using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record ResultMessage(
    LobbyResults Result,
    SacredMsgType Action
) : ISerializable<ResultMessage>
{
    public ResultMessage(ResultMessageData data)
        : this((LobbyResults)data.Result, (SacredMsgType)data.Action) { }

    public static ResultMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out ResultMessageData data);
        return new ResultMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public ResultMessageData ToStruct()
    {
        return new ResultMessageData
        {
            Result = (int)Result,
            Action = (int)Action
        };
    }
}
