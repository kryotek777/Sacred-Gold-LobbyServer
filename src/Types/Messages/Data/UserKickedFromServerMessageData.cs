using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct UserKickedFromServerMessageData
{
    public int PermId;
    public KickReasons Reason;
    public fixed byte Text[Constants.ChatMaxLength];
}
