namespace Sacred.Networking.Types;

/// <summary>
/// <para> Requests public data about a client (Profile information or character savegames) </para>
/// <para> Message: <see cref="SacredMsgType.PublicDataRequest"/> (9) </para>
/// <para> Answer: <see cref="PublicData"/> </para>
/// </summary>
/// <param name="PermId">The ID of the info's owner</param>
/// <param name="BlockId">The ID of the data block that's being requested</param>
/// <param name="Offset">The offset into the data block</param>
/// <param name="Length">The length of the data block</param>
public record PublicDataRequest(
    int PermId,
    short BlockId,
    int Offset,
    int Length
) : ISerializable<PublicDataRequest>
{
    public static PublicDataRequest Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var PermId = reader.ReadInt32();
        var BlockId = reader.ReadInt16();
        var Offset = reader.ReadInt32();
        var Length = reader.ReadInt32();

        return new PublicDataRequest(PermId, BlockId, Offset, Length);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(PermId);
        writer.Write(BlockId);
        writer.Write(Offset);
        writer.Write(Length);

        return ms.ToArray();
    }
}