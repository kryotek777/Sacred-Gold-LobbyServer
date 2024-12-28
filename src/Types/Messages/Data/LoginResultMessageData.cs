using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct LoginResultMessageData
{
    public int Result;
    public fixed byte Ip[4];
    public int PermId;
    public fixed byte Message[Constants.ChatMaxLength];
}
