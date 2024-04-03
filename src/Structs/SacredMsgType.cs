namespace Sacred;

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
    ServerStartInfo = 38,
}
