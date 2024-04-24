namespace Sacred.Networking.Types;

public enum SacredMsgType : ushort
{
    //Incoming
    ClientLoginRequest = 2,
    ReceivePublicData = 10,
    ServerLoginRequest = 12,
    ServerChangePublicInfo = 13,
    ClientCharacterSelect = 17,
    ClientChatMessage = 30,

    //Outgoing
    AcceptClientLogin = 16,
    SendPublicData = 11,
    UpdateServerInfo = 12,
    LobbyResult = 15,
    AcceptServerLogin = 38,
    ClientJoinRoom = 26,
    SendSystemMessage = 31,
    SendPrivateMessage = 33,
    OtherClientJoinedLobby = 28,
    OtherClientLeavedLobby = 29,
    Kick = 34,

    MaybeCustomMessage = 56,
    MaybeLobbyMessage = 48,


}
