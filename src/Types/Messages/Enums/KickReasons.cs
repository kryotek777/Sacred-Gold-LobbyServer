namespace Lobby.Types.Messages;

/// <summary>
/// Reasons for being kicked from a game.
/// </summary>
public enum KickReasons
{
    NotInLobby = 0,
    WrongDataBlock,
    UnknownUser,
    InternalError,
    AlreadyInGame,
    AlreadyInLobby,
    CustomReason
}