using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Lobby.Types;

namespace Lobby;

public class SaveFile
{
    private const int saveHeaderSize = 0x100;
    private const int chunkHeaderSize = 12;
    private const int compressionHeaderSize = 32;
    private const uint compressionMagic = 0xBAADC0DE;

    private SaveHeader _header;
    private List<Chunk> _chunks;

    public SaveFile(byte[] data)
    {
        _chunks = null!;
        Parse(data);

    }

    public SaveFile(string path) : this(File.ReadAllBytes(path)) {}

    private void Parse(ReadOnlySpan<byte> data)
    {
        Utils.FromSpan(data.Slice(0, saveHeaderSize), out SaveHeader saveHeader);

        List<Chunk> chunks = new();

        for (int i = 0; i < saveHeader.chunkCount; i++)
        {
            var headerStart = saveHeaderSize + i * chunkHeaderSize;
            Utils.FromSpan(data.Slice(headerStart, chunkHeaderSize), out ChunkHeader chunkHeader);

            if (IsChunkCompressed(chunkHeader.type))
            {
                Utils.FromSpan(data.Slice(chunkHeader.offset, compressionHeaderSize), out CompressionHeader comprHeader);
                var comprData = data.Slice(chunkHeader.offset + compressionHeaderSize, comprHeader.Length);
                var rawData = Utils.ZLibDecompress(comprData.ToArray());
                chunks.Add(new Chunk(LoadIndex: i, chunkHeader.type, GetUncompressedType(chunkHeader.type), rawData));
            }
            else
            {
                var rawData = data.Slice(chunkHeader.offset, chunkHeader.length).ToArray();
                chunks.Add(new Chunk(LoadIndex: i, chunkHeader.type, GetUncompressedType(chunkHeader.type), rawData));
            }
        }

        _header = saveHeader;
        _chunks = chunks;
    }

    public byte[] GetSaveData()
    {
        Debug.Assert(_header.chunkCount == _chunks.Count);

        using var ms = new MemoryStream();

        ms.Write(_header);

        var chunkCount = _header.chunkCount;
        long dataBaseOffset = saveHeaderSize + chunkHeaderSize * chunkCount;

        for (int i = 0; i < chunkCount; i++)
        {
            var chunk = _chunks[i];

            var chunkHeaderOffset = saveHeaderSize + i * chunkHeaderSize;

            if (IsChunkCompressed(chunk.OriginalType))
            {
                ms.Position = chunkHeaderOffset;

                var chunkHeader = new ChunkHeader()
                {
                    length = chunk.Data.Length,
                    offset = (int)dataBaseOffset,
                    type = chunk.OriginalType
                };

                ms.Write(chunkHeader);

                ms.Position = dataBaseOffset;

                var comprData = Utils.ZLibCompress(chunk.Data);
                var comprHeader = new CompressionHeader()
                {
                    Magic = compressionMagic,
                    Length = comprData.Length,
                };

                ms.Write(comprHeader);
                ms.Write(comprData);

                dataBaseOffset = ms.Position;
            }
            else
            {
                ms.Position = chunkHeaderOffset;

                var chunkHeader = new ChunkHeader()
                {
                    length = chunk.Data.Length,
                    offset = (int)dataBaseOffset,
                    type = chunk.OriginalType
                };

                ms.Write(chunkHeader);

                ms.Position = dataBaseOffset;

                ms.Write(chunk.Data);

                dataBaseOffset = ms.Position;
            }
        }

        return ms.ToArray();
    }

    public void Save(string path)
    {
        var data = GetSaveData();
        File.WriteAllBytes(path, data);
    }

    public CharacterPreview GetCharacterPreview()
    {
        var previewChunk = GetChunk(ChunkType.HeroData2);
        var preview = CharacterPreview.Deserialize(previewChunk.Data);
        return preview;
    }

    public void SetCharacterPreview(CharacterPreview preview)
    {
        var data = preview.Serialize();
        ReplaceChunkData(ChunkType.HeroData2, data);
    }

    private Chunk GetChunk(ChunkType type) => _chunks.First(x => x.Type == type);

    private void ReplaceChunkData(ChunkType type, byte[] data) => ReplaceChunkData(GetChunk(type), data);

    private void ReplaceChunkData(Chunk chunk, byte[] data)
    {
        _chunks[chunk.LoadIndex] = chunk with
        {
            Data = data
        };
    }

    private static bool IsChunkCompressed(ChunkType type)
    {
        switch (type)
        {
            case ChunkType.InventoryZip:
            case ChunkType.ObjectManagerZip:
            case ChunkType.QuestZip:
            case ChunkType.ParticleManagerZip:
            case ChunkType.WeatherZip:
            case ChunkType.WorldZip:
            case ChunkType.BloodZip:
            case ChunkType.RegionZip:
            case ChunkType.StatsZip:
            case ChunkType.SavePictureZip:
            case ChunkType.VariousZip:
            case ChunkType.SaveDescriptionZip:
            case ChunkType.DescriptionZip:
            case ChunkType.AllianceZip:
            case ChunkType.HeroSelfZip:
            case ChunkType.HeroData1Zip:
            case ChunkType.HeroData2Zip:
            case ChunkType.HeroInventoryZip:
            case ChunkType.HeroInventory2Zip:
            case ChunkType.HeroMapZip:
            case ChunkType.HeroStatsZip:
                return true;
            default:
                return false;
        }
    }

    private static ChunkType GetUncompressedType(ChunkType type) => type switch
    {
        ChunkType.InventoryZip => ChunkType.Inventory,
        ChunkType.ObjectManagerZip => ChunkType.ObjectManager,
        ChunkType.QuestZip => ChunkType.Quest,
        ChunkType.ParticleManagerZip => ChunkType.ParticleManager,
        ChunkType.WeatherZip => ChunkType.Weather,
        ChunkType.WorldZip => ChunkType.World,
        ChunkType.BloodZip => ChunkType.Blood,
        ChunkType.RegionZip => ChunkType.Region,
        ChunkType.StatsZip => ChunkType.Stats,
        ChunkType.SavePictureZip => ChunkType.SavePicture,
        ChunkType.VariousZip => ChunkType.Various,
        ChunkType.SaveDescriptionZip => ChunkType.SaveDescription,
        ChunkType.DescriptionZip => ChunkType.Description,
        ChunkType.AllianceZip => ChunkType.Alliance,
        ChunkType.HeroSelfZip => ChunkType.HeroSelf,
        ChunkType.HeroData1Zip => ChunkType.HeroData1,
        ChunkType.HeroData2Zip => ChunkType.HeroData2,
        ChunkType.HeroInventoryZip => ChunkType.HeroInventory,
        ChunkType.HeroInventory2Zip => ChunkType.HeroInventory2,
        ChunkType.HeroMapZip => ChunkType.HeroMap,
        ChunkType.HeroStatsZip => ChunkType.HeroStats,
        _ => type
    };

    [StructLayout(LayoutKind.Explicit, Size = saveHeaderSize)]
    struct SaveHeader
    {
        [FieldOffset(0)]
        public uint signature;

        [FieldOffset(4)]
        public int chunkCount;

        [FieldOffset(8)]
        public int charPreviewSize;

        [FieldOffset(12)]
        public int versionMajor;

        [FieldOffset(16)]
        public int versionMinor;

        [FieldOffset(28)]
        public int language;

        [FieldOffset(32)]
        public int operatingSystem;

        [FieldOffset(36)]
        public int gameVersion;

        [FieldOffset(40)]
        public int executableVersion;
    }

    [StructLayout(LayoutKind.Sequential, Size = chunkHeaderSize, Pack = 1)]
    struct ChunkHeader
    {
        public ChunkType type;
        public int offset;
        public int length;
    }

    [StructLayout(LayoutKind.Explicit, Size = compressionHeaderSize)]
    struct CompressionHeader
    {
        [FieldOffset(0)]
        public uint Magic;

        [FieldOffset(4)]
        public int Length;
    }

    record Chunk(int LoadIndex, ChunkType OriginalType, ChunkType Type, byte[] Data);
}


enum ChunkType : int
{
    Engine = 0x80,
    World = 0x81,
    View = 0x82,
    Description = 0x83,
    Object = 0x85,
    Transition = 0x87,
    Triggers = 0x88,
    Calendar = 0x8B,
    Kernel = 0x8C,
    Inventory = 0x8D,
    ObjectManager = 0x8E,
    DialogVM = 0x8F,
    Diary = 0x90,
    Quest = 0x91,
    Weather = 0x92,
    SavePicture = 0x93,
    Blood = 0x94,
    SaveDescription = 0x95,
    ParticleManager = 0x96,
    MouseDD = 0x98,
    Region = 0x99,
    Alliance = 0x9A,
    Stats = 0x9B,
    UIState = 0x9C,
    Various = 0x9D,

    InventoryZip = 0x9F,
    ObjectManagerZip = 0xA0,
    QuestZip = 0xA1,
    ParticleManagerZip = 0xA2,
    WeatherZip = 0xA3,
    WorldZip = 0xA4,
    BloodZip = 0xA5,
    RegionZip = 0xA6,
    StatsZip = 0xA7,
    SavePictureZip = 0xA8,
    VariousZip = 0xA9,
    SaveDescriptionZip = 0xAA,
    DescriptionZip = 0xAB,
    AllianceZip = 0xAC,

    HeroSelf = 0xC0,
    HeroInventory = 0xC1,
    HeroData1 = 0xC2,
    HeroData2 = 0xC3,
    HeroData3 = 0xC4,
    HeroInventory2 = 0xC6,

    HeroSelfZip = 0xC7,
    HeroData1Zip = 0xC8,
    HeroData2Zip = 0xC9,
    HeroInventoryZip = 0xCA,
    HeroInventory2Zip = 0xCB,

    HeroMap = 0xCC,
    HeroMapZip = 0xCD,
    HeroStats = 0xCE,
    HeroStatsZip = 0xCF,

    Description2 = 0xE0,
    RegionKill = 0xE1,
    ComplexData = 0xE2,
}