namespace Lobby.Types.Messages;

[Flags]
public enum ChannelFlags
{
    None = 0,
    ClosedNet = 1 << 0,
    Noobs = 1 << 1,
    Bronze = 1 << 2,
    Silver = 1 << 3,
    Gold = 1 << 4,
    Platinum = 1 << 5,
    Niobium = 1 << 6,
    Cheater = 1 << 7,
    Hardcore = 1 << 8,
    Clan = 1 << 9,
    Special = 1 << 10,
    VIP = 1 << 11,
    Private = 1 << 12
}