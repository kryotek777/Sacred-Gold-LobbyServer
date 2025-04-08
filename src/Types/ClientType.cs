namespace Lobby.Networking;

[Flags]
public enum ClientType
{
    None = 0,
    Unknown = 1,
    User = 2,
    Server = 4
}