namespace Sacred.Networking.Types;

/// <summary>
/// <para> Represents a block of public data, the contents and format depend on the BlockId </para>
/// <para> BlockId <see cref="Constants.ProfileBlockId"/> (10) represents <see cref="ProfileData"/> </para>
/// <para> BlockIds <= 8 represent character previews and savegames </para>
/// <para> Messages: <see cref="SacredMsgType.ReceivePublicData"/> (10) and <see cref="SacredMsgType.SendPublicData"/> (11) </para>
/// </summary>
/// <param name="PermId"></param>
/// <param name="BlockId"></param>
/// <param name="DownloadSize"></param>
/// <param name="UploadOffset"></param>
/// <param name="UploadLength"></param>
/// <param name="Data"></param>
public record PublicData(
    int PermId,
    short BlockId,
    int DownloadSize,
    int UploadOffset,
    int UploadLength,
    byte[] Data
) : ISerializable<PublicData>
{
    public static PublicData Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var PermId = reader.ReadInt32();
        var BlockId = reader.ReadInt16();
        var Size = reader.ReadInt32();
        var Offset = reader.ReadInt32();
        var Length = reader.ReadInt32();
        var Data = reader.ReadBytes(Size);

        return new PublicData(PermId, BlockId, Size, Offset, Length, Data);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(PermId);
        writer.Write(BlockId);
        writer.Write(DownloadSize);
        writer.Write(UploadOffset);
        writer.Write(UploadLength);
        writer.Write(Data);

        return ms.ToArray();
    }

    public ProfileData ReadProfileData()
    {
        var profileBytes = Data.AsSpan(4).ToArray();
        var uncompressed = Utils.ZLibDecompress(profileBytes);
        var profileData = ProfileData.Deserialize(uncompressed);
        return profileData;
    }

    public static PublicData FromProfileData(int permId, ProfileData profileData)
    {
        //Serialize and compress the profile data
        var serialized = profileData.Serialize();
        var compressed = Utils.ZLibCompress(serialized);

        //The format is the uncompressed size of the data followed by the compressed data
        var finalData = new byte[compressed.Length + 4];
        BitConverter.TryWriteBytes(finalData, serialized.Length);
        Array.Copy(compressed, 0, finalData, 4, compressed.Length);

        //Build the final PublicData
        var publicData = new PublicData(permId, Constants.ProfileBlockId, 0, 0, finalData.Length, finalData);

        return publicData;
    }
}