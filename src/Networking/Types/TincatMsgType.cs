namespace Lobby.Networking.Types;

public enum TincatMsgType : uint
{
    TIMESYNC = 1,
    CUSTOMDATA = 2,
    LOGMEON = 3,
    LOGMEOFF = 4,
    LOGONACCEPTED = 5,
    LOGOFFACCEPTED = 6,
    //MaybeUserRemove = 8,
    //MaybeUserInit = 9,
    STAYINGALIVE = 11,
    //MaybeDistributeFile = 12,
    //MaybeDistributeGroupData = 14,
}