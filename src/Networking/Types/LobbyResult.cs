namespace Sacred.Networking.Types;

/// <summary>
/// Generic answer to a message
/// </summary>
/// <param name="Result">The result code</param>
/// <param name="AnsweringTo">The message code we're answering to</param>
public record LobbyResult(LobbyResults Result, SacredMsgType AnsweringTo) : ISerializable<LobbyResult>
{
    public static LobbyResult Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var result = (LobbyResults)reader.ReadInt32();
        var answeringTo = (SacredMsgType)reader.ReadInt32();

        return new LobbyResult(result, answeringTo);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((int)Result);
        writer.Write((int)AnsweringTo);

        return ms.ToArray();
    }
}