namespace Sacred.Networking.Types;

public enum SacredMsgType : ushort
{
    //Incoming
    ClientLoginRequest = 2,
    ServerLoginRequest = 12,
    ServerChangePublicInfo = 13,
    ClientCharacterSelect = 17,
    ClientChatMessage = 30,

    //Outgoing
    AcceptClientLogin = 16,
    UpdateServerInfo = 12,
    LobbyResult = 15,
    AcceptServerLogin = 38,
    ClientJoinRoom = 26,
    SendSystemMessage = 31,
    SendPrivateMessage = 33,
    OtherClientJoinedLobby = 28,
    OtherClientLeavedLobby = 29,

    MaybeCustomMessage = 56,
    MaybeLobbyMessage = 48,


}
