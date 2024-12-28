namespace Lobby.Types;

public record struct TincatHeader(uint Magic, uint Source, uint Destination, TincatMsgType Type, uint Unknown, int Length, uint Checksum)
{
    public void Serialize(ref SpanWriter writer)
    {
        writer.WriteUInt32(Magic);
        writer.WriteUInt32(Source);
        writer.WriteUInt32(Destination);
        writer.WriteUInt32((uint)Type);
        writer.WriteUInt32(Unknown);
        writer.WriteInt32(Length);
        writer.WriteUInt32(Checksum);
    }

    public void Serialize(Stream stream)
    {
        stream.WriteUInt32(Magic);
        stream.WriteUInt32(Source);
        stream.WriteUInt32(Destination);
        stream.WriteUInt32((uint)Type);
        stream.WriteUInt32(Unknown);
        stream.WriteInt32(Length);
        stream.WriteUInt32(Checksum);
    }

    public static void Deserialize(ref SpanReader reader, out TincatHeader result)
    {
        var Magic = reader.ReadUInt32();
        var Source = reader.ReadUInt32();
        var Destination = reader.ReadUInt32();
        var Type = (TincatMsgType)reader.ReadUInt32();
        var Unknown = reader.ReadUInt32();
        var Length = reader.ReadInt32();
        var Checksum = reader.ReadUInt32();

        result = new TincatHeader(Magic, Source, Destination, Type, Unknown, Length, Checksum);
    }
}