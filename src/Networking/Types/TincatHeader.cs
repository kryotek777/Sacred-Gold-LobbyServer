using System.Runtime.InteropServices;
using Sacred.Networking.Structs;

namespace Sacred.Networking.Types;

public class TincatHeader
{
    public const int DataSize = TincatHeaderData.DataSize;
    public const uint TincatMagic = 0xDABAFBEF;
    public const uint ServerId = 0xEFFFFFCC;
    public const uint UnconnectedClientId = 0xEFFFFFCC;

    public TincatHeaderData Data;

    public uint Magic
    {
        get => Data.magic;
        set => Data.magic = value;
    }

    public uint From
    {
        get => Data.from;
        set => Data.from = value;
    }

    public uint To
    {
        get => Data.to;
        set => Data.to = value;
    }

    public TincatMsgType Type
    {
        get => (TincatMsgType)Data.msgType;
        set => Data.msgType = (ushort)value;
    }

    public uint Unknown
    {
        get => Data.unknown;
        set => Data.unknown = value;
    }

    public int PayloadLength
    {
        get => Data.payloadLength;
        set => Data.payloadLength = value;
    }

    public uint Checksum
    {
        get => Data.checksum;
        set => Data.checksum = value;
    }

    public TincatHeader(uint from, uint to, TincatMsgType type, int payloadLength, uint checksum, uint unknown = 0)
    {
        Magic = TincatMagic;
        From = from;
        To = to;
        Type = type;
        Unknown = 0;
        PayloadLength = payloadLength;
        Checksum = checksum;
    }

    public TincatHeader(in TincatHeaderData data)
    {
        Data = data;
    }

    public TincatHeader(ReadOnlySpan<byte> data)
    {
        Data = MemoryMarshal.Read<TincatHeaderData>(data);
    }

    public override string ToString()
    {
        return
            $"Magic: {Magic:X}\n" +
            $"From: {From:X}\n" +
            $"To: {To:X}\n" +
            $"Type: {Type}\n" +
            $"Unknown: {Unknown:X}\n" +
            $"Payload Length: {PayloadLength}\n" +
            $"Checksum: {Checksum:X}\n";
    }
}