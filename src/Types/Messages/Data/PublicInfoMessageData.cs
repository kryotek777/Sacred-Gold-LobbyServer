using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PublicInfoMessageData
{
    public int PermId;
    public fixed byte Username[Constants.NameMaxLength];
}
