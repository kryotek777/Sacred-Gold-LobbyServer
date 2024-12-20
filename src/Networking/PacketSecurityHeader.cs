namespace Lobby.Networking.Types;

public record struct PacketSecurityHeader(SacredMsgType Type, uint SecurityKey, int Length, uint Checksum)
{
    public void Serialize(ref SpanWriter writer)
    {
        writer.WriteUInt16((ushort)Type);
        writer.WriteUInt32(SecurityKey);
        writer.WriteInt32(Length);
        writer.WriteUInt32(Checksum);
    }

    public static PacketSecurityHeader Deserialize(ref SpanReader reader)
    {
        var Type = (SacredMsgType)reader.ReadUInt16();
        var SecurityKey = reader.ReadUInt32();
        var PacketLength = reader.ReadInt32();
        var Checksum = reader.ReadUInt32();

        return new PacketSecurityHeader(Type, SecurityKey, PacketLength, Checksum);
    }
}
