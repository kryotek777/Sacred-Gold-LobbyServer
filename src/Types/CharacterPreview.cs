using System.Diagnostics;
using Lobby.Types;

namespace Lobby.Types;

public record CharacterPreview(
    uint Level,
    uint Type,
    string Name,
    uint EquippedItemCount,
    uint[] EquippedItems,
    byte[] GameCompletions,
    bool Hardcore,
    bool HasDied,
    byte Flags,
    byte InventoryItemsCount,
    uint Experience,
    uint Gold,
    byte SaveYear,
    byte SaveMonth,
    byte SaveDay,
    byte[] Effects
) : ISerializable<CharacterPreview>
{
    public static readonly CharacterPreview Empty = Deserialize(new byte[556]);

    public static CharacterPreview Deserialize(ReadOnlySpan<byte> span)
    {
        var reader = new SpanReader(span);

        var Level = reader.ReadUInt32();
        var Type = reader.ReadUInt32();
        var Name = Utils.Utf16ToString(reader.ReadBytes(64 * 2));
        var EquippedItemCount = reader.ReadUInt32();
        var EquippedItems = new uint[24];

        for (int i = 0; i < EquippedItems.Length; i++)
        {
            EquippedItems[i] = reader.ReadUInt32();
        }

        var GameCompletions = reader.ReadBytes(7).ToArray();
        var Hardcore = reader.ReadBoolean();
        reader.ReadBytes(5);    // Reserved
        var HasDied = reader.ReadBoolean();
        var Flags = reader.ReadByte();
        var InventoryItemsCount = reader.ReadByte();
        var Experience = reader.ReadUInt32();
        var Gold = reader.ReadUInt32();
        var SaveYear = reader.ReadByte();
        var SaveMonth = reader.ReadByte();
        var SaveDay = reader.ReadByte();
        reader.ReadBytes(5);    // Reserved

        var Effects = reader.ReadBytes(12 * 24).ToArray();

        return new CharacterPreview(
            Level,
            Type,
            Name,
            EquippedItemCount,
            EquippedItems,
            GameCompletions,
            Hardcore,
            HasDied,
            Flags,
            InventoryItemsCount,
            Experience,
            Gold,
            SaveYear,
            SaveMonth,
            SaveDay,
            Effects
        );
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();

        stream.Write(Level);
        stream.Write(Type);
        stream.Write(Utils.StringToUtf16(Name, 64 * 2));
        stream.Write(EquippedItemCount);

        Debug.Assert(EquippedItems.Length == 24);
        for (int i = 0; i < EquippedItems.Length; i++)
        {
            stream.Write(EquippedItems[i]);
        }

        stream.Write(GameCompletions);
        stream.Write(Hardcore);
        stream.Position += 5;
        stream.Write(HasDied);
        stream.Write(Flags);
        stream.Write(InventoryItemsCount);
        stream.Write(Experience);
        stream.Write(Gold);
        stream.Write(SaveYear);
        stream.Write(SaveMonth);
        stream.Write(SaveDay);
        stream.Position += 5;
        stream.Write(Effects);

        return stream.ToArray();
    }
}