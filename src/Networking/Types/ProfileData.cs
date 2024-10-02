using System.Text;

namespace Sacred.Networking.Types;

/// <summary>
/// <para> Public information about a player's profile </para>
/// <para> Used as a payload in <see cref="PublicData"/> </para>
/// </summary>
public record ProfileData(
    ushort Version,
    string Account,
    string Name,
    string Nick,
    string Clan,
    string Page,
    string Icq,
    string Text,
    string Email,
    int PermId,
    bool ShowEmail,
    byte Slot,
    string[] LobbyCharNames,
    byte[] HeroPreviewData

) : ISerializable<ProfileData>
{
    public static ProfileData Deserialize(ReadOnlySpan<byte> span)
    {
        using var reader = new BinaryReader(new MemoryStream(span.ToArray()));

        var Version = reader.ReadUInt16();
        var Account = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileStringLength));
        var Name = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileStringLength));
        var Nick = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileStringLength));
        var Clan = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileStringLength));
        var Page = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileStringLength));
        var Icq = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileStringLength));
        var Text = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileTextLength));
        var Email = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileStringLength));
        var PermId = reader.ReadInt32();
        var Slot = reader.ReadByte();
        var ShowEmail = reader.ReadBoolean();
        var LobbyCharNames = new string[6];
        
        for (int i = 0; i < LobbyCharNames.Length; i++)
        {
            LobbyCharNames[i] = Encoding.Unicode.GetString(reader.ReadBytes(Constants.Utf16ProfileStringLength));
        }
        
        var HeroPreviewData = reader.ReadBytes(880);

        return new ProfileData(Version, Account, Name, Nick, Clan, Page, Icq, Text, Email, PermId, ShowEmail, Slot, LobbyCharNames, HeroPreviewData);
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Version);
        writer.Write(Utils.StringToUtf16(Account, Constants.Utf16ProfileStringLength));
        writer.Write(Utils.StringToUtf16(Name, Constants.Utf16ProfileStringLength));
        writer.Write(Utils.StringToUtf16(Nick, Constants.Utf16ProfileStringLength));
        writer.Write(Utils.StringToUtf16(Clan, Constants.Utf16ProfileStringLength));
        writer.Write(Utils.StringToUtf16(Page, Constants.Utf16ProfileStringLength));
        writer.Write(Utils.StringToUtf16(Icq, Constants.Utf16ProfileStringLength));
        writer.Write(Utils.StringToUtf16(Text, Constants.Utf16ProfileTextLength));
        writer.Write(Utils.StringToUtf16(Email, Constants.Utf16ProfileStringLength));
        writer.Write(PermId);
        writer.Write(Slot);
        writer.Write(ShowEmail);

        for (int i = 0; i < LobbyCharNames.Length; i++)
        {
            writer.Write(Utils.StringToUtf16(LobbyCharNames[i], Constants.Utf16ProfileStringLength));
        }

        writer.Write(HeroPreviewData);

        return ms.ToArray();
    }

    public static ProfileData CreateEmpty(int permId)
    {
        return new ProfileData(4, "", "", "", "", "", "", "", "", permId, false, 0, [ "", "", "", "", "", "" ], new byte[880]);
    }
}