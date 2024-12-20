namespace Lobby.Networking.Types;

[Flags]
public enum ServerFlags
{
    Dedicated = 1 << 0,
    HasPassword = 1 << 1,
    Locked = 1 << 2,
    Preconfigured = 1 << 3,
    Campaign = 1 << 4,
    FreeGame = 1 << 5,
    PlayerKiller = 1 << 6,
    Running = 1 << 7,
    Silver = 1 << 8,
    Gold = 1 << 9,
    Platinum = 1 << 10,
    Niobium = 1 << 11,
    Clan = 1 << 12,
    Special = 1 << 13,
    Vip = 1 << 14,
    HasSavegame = 1 << 15,
    Starting = 1 << 16,
    Invisible = 1 << 17,
    Underworld = 1 << 18,
}