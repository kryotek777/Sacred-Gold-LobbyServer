using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct KickMessageData
{
    public fixed byte Reason[Constants.ChatMaxLength];
}
