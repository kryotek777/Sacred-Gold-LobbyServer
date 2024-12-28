using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record LogAlarmMessage() : ISerializable<LogAlarmMessage>
{
    public LogAlarmMessage(LogAlarmMessageData data) : this() { }

    public static LogAlarmMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out LogAlarmMessageData data);
        return new LogAlarmMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public LogAlarmMessageData ToStruct() => new();
}
