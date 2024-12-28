using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MotdMessageData
{

    public ushort Id;

    public ushort Length;

    public fixed byte Reserved[128];

    public fixed byte Text[Constants.MotdMaxLength];
}