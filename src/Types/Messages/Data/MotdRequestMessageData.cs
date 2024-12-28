using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MotdRequestMessageData
{

    public ushort Id;

    public fixed byte Reserved[128];
}
