using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PublicDataMessageData
{
    public int PermId;
    public short BlockId;
    public int Size;
    public int Offset;
    public int Length;
    public fixed byte Data[Constants.PublicDataMax];
}
