namespace Lobby.Types.Messages;

public unsafe record EmptyMessage() : ISerializable<EmptyMessage>
{
    public static EmptyMessage Deserialize(ReadOnlySpan<byte> span) => new();

    public byte[] Serialize() => Array.Empty<byte>();
}
