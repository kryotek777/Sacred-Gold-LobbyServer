namespace Lobby;

public enum BanType
{
    //Everything is allowed
    None,

    //Fully ban the IP address from even attempting to connect
    Full,

    //Forbid only game clients
    ClientOnly,

    //Forbid only game servers
    ServerOnly,
}
