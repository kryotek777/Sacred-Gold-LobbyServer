using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PublicDataRequestMessageData
{
    public int PermId;
    public short BlockId;
    public int Offset;
    public int Length;
}
