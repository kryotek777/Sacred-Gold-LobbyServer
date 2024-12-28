using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PrivateInfoMessageData
{
    public fixed byte Password[Constants.PasswordMaxLength];
    public fixed byte Mail[Constants.MailMaxLength];
}
