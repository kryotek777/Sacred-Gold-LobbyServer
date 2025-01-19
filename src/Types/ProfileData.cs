using Lobby.Types;

namespace Lobby.Types;

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
    uint ConnectionSpeed,
    bool ShowEmail,
    int SelectedCharacterSlot,
    string[] CharactersNames,
    CharacterPreview SelectedCharacter
) : ISerializable<ProfileData>
{
    private const int Utf16ProfileStringLength = 160;
    private const int Utf16ProfileTextLength = 512;
    private const int CharacterNamesCount = 8;

    public static ProfileData Deserialize(ReadOnlySpan<byte> span)
    {
        var reader = new SpanReader(span);

        var Version = reader.ReadUInt16();
        var Account = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileStringLength));
        var Name = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileStringLength));
        var Nick = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileStringLength));
        var Clan = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileStringLength));
        var Page = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileStringLength));
        var Icq = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileStringLength));
        var Text = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileTextLength));
        var Email = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileStringLength));
        var PermId = reader.ReadInt32();
        var ConnectionSpeed = reader.ReadUInt32();
        var ShowEmail = reader.ReadBoolean();
        var SelectedCharacterSlot = reader.ReadByte();
        var CharactersNames = new string[CharacterNamesCount];

        for (int i = 0; i < CharacterNamesCount; i++)
        {
            CharactersNames[i] = Utils.Utf16ToString(reader.ReadBytes(Utf16ProfileStringLength));
        }

        var SelectedCharacter = CharacterPreview.Deserialize(reader.ReadAll());

        return new ProfileData(
            Version,
            Account,
            Name,
            Nick,
            Clan,
            Page,
            Icq,
            Text,
            Email,
            PermId,
            ConnectionSpeed,
            ShowEmail,
            SelectedCharacterSlot,
            CharactersNames,
            SelectedCharacter
        );
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();

        stream.Write(Version);
        stream.Write(Utils.StringToUtf16(Account, Utf16ProfileStringLength));
        stream.Write(Utils.StringToUtf16(Name, Utf16ProfileStringLength));
        stream.Write(Utils.StringToUtf16(Nick, Utf16ProfileStringLength));
        stream.Write(Utils.StringToUtf16(Clan, Utf16ProfileStringLength));
        stream.Write(Utils.StringToUtf16(Page, Utf16ProfileStringLength));
        stream.Write(Utils.StringToUtf16(Icq, Utf16ProfileStringLength));
        stream.Write(Utils.StringToUtf16(Text, Utf16ProfileTextLength));
        stream.Write(Utils.StringToUtf16(Email, Utf16ProfileStringLength));
        stream.Write(PermId);
        stream.Write(ConnectionSpeed);
        stream.Write(ShowEmail);
        stream.Write((byte)SelectedCharacterSlot);

        for (int i = 0; i < CharactersNames.Length; i++)
        {
            stream.Write(Utils.StringToUtf16(CharactersNames[i], Utf16ProfileStringLength));
        }

        stream.Write(SelectedCharacter.Serialize());

        return stream.ToArray();
    }

    public static ProfileData CreateEmpty(int permId) =>
    new ProfileData(
            Version: 4,
            "Account",
            "Name",
            "Nick",
            "Clan",
            "Page",
            "Icq",
            "Text",
            "Email",
            permId,
            ConnectionSpeed: 0,
            ShowEmail: false,
            SelectedCharacterSlot: 0,
            CharactersNames: ["Char1", "Char2", "Char3", "Char4", "Char5", "Char6", "Char7", "Char8"],
            SelectedCharacter: CharacterPreview.Empty
    );
}