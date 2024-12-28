using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ChatMessageData
{
    public fixed byte SenderName[Constants.NameMaxLength];
    public int SenderPermId;
    public int DestinationPermId;
    public fixed byte Text[Constants.ChatMaxLength];
}
