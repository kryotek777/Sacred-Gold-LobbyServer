using System.Runtime.InteropServices;

namespace Lobby.Types.Messages.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct LoginMessageData
{
    public fixed byte Username[Constants.NameMaxLength];
    public fixed byte Password[Constants.PasswordMaxLength];
    public fixed byte CdKey[Constants.KeyMaxLength];
    public uint PatchLevel;
    public uint ProgramVersion;
    public fixed byte CdKey2[Constants.KeyMaxLength];
}
