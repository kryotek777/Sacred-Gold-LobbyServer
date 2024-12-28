using Lobby.Types.Messages.Data;

namespace Lobby.Types.Messages;

public unsafe record PublicDataMessage(
    int PermId,
    short BlockId,
    int Size,
    int Offset,
    int Length,
    byte[] Data
) : ISerializable<PublicDataMessage>
{
    public PublicDataMessage(PublicDataMessageData data)
        : this(
            data.PermId,
            data.BlockId,
            data.Size,
            data.Offset,
            data.Length,
            new ReadOnlySpan<byte>(data.Data, Constants.PublicDataMax).ToArray()
        )
    { }

    public static PublicDataMessage Deserialize(ReadOnlySpan<byte> span)
    {
        Utils.FromSpan(span, out PublicDataMessageData data);
        return new PublicDataMessage(data);
    }

    public byte[] Serialize()
    {
        var data = ToStruct();
        return Utils.ToArray(in data);
    }

    public PublicDataMessageData ToStruct()
    {
        PublicDataMessageData result = new();
        result.PermId = PermId;
        result.BlockId = BlockId;
        result.Size = Size;
        result.Offset = Offset;
        result.Length = Length;
        Data.CopyTo(new Span<byte>(result.Data, Constants.PublicDataMax));
        return result;
    }

    public ProfileData ReadProfileData()
    {
        var profileBytes = Data.AsSpan(4).ToArray();
        var uncompressed = Utils.ZLibDecompress(profileBytes);
        var profileData = ProfileData.Deserialize(uncompressed);
        return profileData;
    }

    public static PublicDataMessage FromProfileData(int permId, ProfileData profileData)
    {
        //Serialize and compress the profile data
        var serialized = profileData.Serialize();
        var compressed = Utils.ZLibCompress(serialized);

        //The format is the uncompressed size of the data followed by the compressed data
        var finalData = new byte[compressed.Length + 4];
        BitConverter.TryWriteBytes(finalData, serialized.Length);
        Array.Copy(compressed, 0, finalData, 4, compressed.Length);

        //Build the final PublicData
        var publicData = new PublicDataMessage(permId, Constants.ProfileBlockId, 0, 0, finalData.Length, finalData);

        return publicData;
    }
}
