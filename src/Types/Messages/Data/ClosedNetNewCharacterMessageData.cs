using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ClosedNetNewCharacterMessageData
{
    public short BlockId;
    public short TemplateId;
}
