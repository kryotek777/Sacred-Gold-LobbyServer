using System.Runtime.InteropServices;
using Sacred.Networking.Structs;

namespace Sacred.Networking.Types;

public class SacredHeader
{
    public const int DataSize = SacredHeaderData.DataSize;
    public const int SacredMagic = 0x26B6;

    public SacredHeaderData Data;

    public ushort Magic
    {
        get => Data.magic;
        set => Data.magic = value;
    }

    public SacredMsgType Type1
    {
        get => (SacredMsgType)Data.type1;
        set => Data.type1 = (ushort)value;
    }

    public SacredMsgType Type2
    {
        get => (SacredMsgType)Data.type2;
        set => Data.type2 = (ushort)value;
    }

    public uint Unknown1
    {
        get => Data.unknown1;
        set => Data.unknown1 = value;
    }

    public int PayloadLength
    {
        get => Data.payloadLength;
        set => Data.payloadLength = value;
    }

    public uint Unknown2
    {
        get => Data.unknown2;
        set => Data.unknown2 = value;
    }


    public SacredHeader(SacredMsgType type, int payloadLength)
    {
        Magic = SacredMagic;
        Type1 = type;
        Type2 = type;
        Unknown1 = 0;
        PayloadLength = payloadLength;
        Unknown2 = 0;
    }

    public SacredHeader(in SacredHeaderData data)
    {
        Data = data;
    }

    public SacredHeader(ReadOnlySpan<byte> data)
    {
        Data = MemoryMarshal.Read<SacredHeaderData>(data);
    }

    public byte[] ToArray()
    {
        unsafe
        {
            fixed (SacredHeaderData* data = &Data)
            {
                var arr = new byte[DataSize];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = data->rawData[i];
                }
                return arr;
            }
        }
    }

    public override string ToString()
    {
        var type1 = Enum.IsDefined(Type1) ? Type1.ToString() : ((int)Type1).ToString();
        var type2 = Enum.IsDefined(Type2) ? Type2.ToString() : ((int)Type2).ToString();

        return
            $"Magic: {Magic:X}\n" +
            $"Type 1: {type1}\n" +
            $"Type 2: {type2}\n" +
            $"Data Length: {PayloadLength}\n" +
            $"Unknown 1: {Unknown1:X}\n" +
            $"Unknown 2: {Unknown1:X}\n";

    }
}