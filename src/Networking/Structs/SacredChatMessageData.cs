using System.Runtime.InteropServices;

namespace Sacred;

[StructLayout(LayoutKind.Explicit, Size = DataSize)]
public unsafe struct SacredChatMessageData
{
    public const int DataSize = 344;

    [FieldOffset(0)]
    public fixed byte senderText[80];

    [FieldOffset(80)]
    public uint senderId;

    [FieldOffset(84)]
    public int isPrivateMessage;

    [FieldOffset(88)]
    public fixed byte messageText[128];

    [FieldOffset(0)]
    public fixed byte rawData[DataSize];
} 