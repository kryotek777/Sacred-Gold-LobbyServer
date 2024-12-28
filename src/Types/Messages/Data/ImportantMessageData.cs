using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ImportantMessageData
{

    public bool VeryImportant;

    public fixed byte MessageData[Constants.ChatMaxLength];

    public int DestinationPermId;
}
