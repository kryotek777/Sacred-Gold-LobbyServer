using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ResultMessageData
{
    public int Result;
    public int Action;
}
