namespace Lobby.Networking.Types;

public enum LobbyResults
{
    //OK.
    Ok = 0,
    //Wrong Version!
    WrongVersion = 1,
    //Could not change private info!
    ChangePrivateInfoFailed = 2,
    //Private info changed.
    ChangePrivateInfoSuccess = 3,
    //Could not change public info!
    ChangePublicInfoFailed = 4,
    //Public info changed.
    ChangePublicInfoSuccess = 5,
    //Invalid Sacred CD-Key!
    InvalidSacredCDKey = 6,
    //Invalid password!
    InvalidPassword = 7,
    //Unknown user!
    ErrorUnknownUser = 8,
    //Username already exists!
    ErrorUsernameExists = 9,
    //User is already online!
    ErrorUserAlreadyOnline = 10,
    //Could not change public data!
    ChangePublicDataFailed = 11,
    //Public data changed.
    ChangePublicDataSuccess = 12,
    //Invalid block requested!
    ErrorInvalidBlockRequested = 13,
    //Internal error!
    InternalError = 14,
    //Game Login not allowed!
    GameLoginNotAllowed = 15,
    //Game Update not allowed!
    GameUpdateNotAllowed = 16,
    //Game Remove not allowed!
    GameRemoveNotAllowed = 17,
    //Invalid block selected!
    InvalidBlockSelected = 18,
    //Could not join channel!
    ErrorJoiningChannel = 19,
    //Server full, try again later!
    ErrorServerFull = 20,
    //Account is not active!
    ErrorAccountInactive = 21,
    //Sacred CD-Key is already in use!
    ErrorSacredCDKeyInUse = 22,
    //GSR_RESERVED1!
    GSR_RESERVED1 = 23,
    //GSR_RESERVED2!
    GSR_RESERVED2 = 24,
    //GSR_RESERVED3!
    GSR_RESERVED3 = 25,
    //GSR_RESERVED4!
    GSR_RESERVED4 = 26,
    //GSR_RESERVED5!
    GSR_RESERVED5 = 27,
    //User not found!
    ErrorUserNotFound = 28,
    //Sender not found!
    ErrorSenderNotFound = 29,
    //User was banned!
    ErrorUserBanned = 30,
    //Backconnect failed!
    ErrorBackconnectFailed = 31,
    //Not a game!
    ErrorNotAGame = 32,
    //Invalid AddOn CD-Key!
    ErrorInvalidAddonCDKey = 33,
    //AddOn CD-Key is already in use!
    ErrorAddonCDKeyInUse = 34,
}