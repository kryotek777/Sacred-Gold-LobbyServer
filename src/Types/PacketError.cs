namespace Lobby.Networking;

public enum PacketError
{
    None,
    WrongMagic,
    WrongChecksum,
    PacketUnexpected,
    WrongModuleId,
    SecurityTypeMismatch,
    SecurityWrongKey,
    SecurityNotAllowed,
    SecurityLengthMismatch
}