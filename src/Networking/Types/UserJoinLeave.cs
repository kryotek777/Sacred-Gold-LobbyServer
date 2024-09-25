namespace Sacred.Networking.Types;

public record UserJoinLeave(
    int PermId,
    string DisplayName
) : ISerializable<UserJoinLeave>
{
    public static UserJoinLeave Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var PermId = reader.ReadInt32();
        var DisplayName = Utils.Win1252ToString(reader.ReadBytes(Constants.UsernameMaxLength));

        return new UserJoinLeave(PermId, DisplayName);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(PermId);
        writer.Write(Utils.StringToWin1252(DisplayName).PadToSize(80));

        return ms.ToArray();
    }
}