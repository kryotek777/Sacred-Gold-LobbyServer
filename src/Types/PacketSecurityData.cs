using Lobby.Networking;
using Lobby.Types;
using Lobby.Types.Messages;
using Lobby.Types.Messages.Data;

public record struct PacketSecurityData(Type MessageType, Type? MessageTypeRaw, int Length, uint SecurityKey, bool DynamicSize, ClientType AllowedClients)
{
    public static PacketSecurityData Get(SacredMsgType Type)
    {
        return PacketDictionary[Type];
    }

    private static Dictionary<SacredMsgType, PacketSecurityData> PacketDictionary = new Dictionary<SacredMsgType, PacketSecurityData> {
        { SacredMsgType.ClientRegistrationRequest, new(MessageType: typeof(RegistrationMessage), MessageTypeRaw: typeof(RegistrationMessageData), Length: 256, SecurityKey: 0xDDCCBB01, DynamicSize: false, AllowedClients: ClientType.Unknown)},
        { SacredMsgType.ClientLoginRequest, new(MessageType: typeof(LoginMessage), MessageTypeRaw: typeof(LoginMessageData), Length: 176, SecurityKey: 0xDDCCBB02, DynamicSize: false, AllowedClients: ClientType.Unknown)},
        { SacredMsgType.PrivateInfoRequest, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 14, SecurityKey: 0xDDCCBB03, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.ReceivePrivateInfo, new(MessageType: typeof(PrivateInfoMessage), MessageTypeRaw: typeof(PrivateInfoMessageData), Length: 126, SecurityKey: 0xDDCCBB04, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.SendPrivateInfo, new(MessageType: typeof(PrivateInfoMessage), MessageTypeRaw: typeof(PrivateInfoMessageData), Length: 126, SecurityKey: 0xDDCCBB05, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.PublicInfoRequest, new(MessageType: typeof(PublicInfoRequestMessage), MessageTypeRaw: typeof(PublicInfoRequestMessageData), Length: 18, SecurityKey: 0xDDCCBB06, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.ReceivePublicInfo, new(MessageType: typeof(PublicInfoMessage), MessageTypeRaw: typeof(PublicInfoMessageData), Length: 98, SecurityKey: 0xDDCCBB07, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.SendPublicInfo, new(MessageType: typeof(PublicInfoMessage), MessageTypeRaw: typeof(PublicInfoMessageData), Length: 98, SecurityKey: 0xDDCCBB08, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.PublicDataRequest, new(MessageType: typeof(PublicDataRequestMessage), MessageTypeRaw: typeof(PublicDataRequestMessageData), Length: 28, SecurityKey: 0xDDCCBB09, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.ReceivePublicData, new(MessageType: typeof(PublicDataMessage), MessageTypeRaw: typeof(PublicDataMessageData), Length: 131104, SecurityKey: 0xDDCCBB10, DynamicSize: true, AllowedClients: ClientType.User)},
        { SacredMsgType.SendPublicData, new(MessageType: typeof(PublicDataMessage), MessageTypeRaw: typeof(PublicDataMessageData), Length: 131104, SecurityKey: 0xDDCCBB11, DynamicSize: true, AllowedClients: ClientType.None)},
        { SacredMsgType.ServerLoginRequest, new(MessageType: typeof(ServerInfoMessage), MessageTypeRaw: typeof(ServerInfoMessageData), Length: 130, SecurityKey: 0xDDCCBB12, DynamicSize: false, AllowedClients: ClientType.Unknown)},
        { SacredMsgType.ServerChangePublicInfo, new(MessageType: typeof(ServerInfoMessage), MessageTypeRaw: typeof(ServerInfoMessageData), Length: 130, SecurityKey: 0xDDCCBB13, DynamicSize: false, AllowedClients: ClientType.Server)},
        { SacredMsgType.ServerLogout, new(MessageType: typeof(ServerInfoMessage), MessageTypeRaw: typeof(ServerInfoMessageData), Length: 130, SecurityKey: 0xDDCCBB14, DynamicSize: false, AllowedClients: ClientType.Server)},
        { SacredMsgType.LobbyResult, new(MessageType: typeof(ResultMessage), MessageTypeRaw: typeof(ResultMessageData), Length: 22, SecurityKey: 0xDDCCBB15, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ClientLoginResult, new(MessageType: typeof(LoginResultMessage), MessageTypeRaw: typeof(LoginResultMessageData), Length: 282, SecurityKey: 0xDDCCBB16, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ClientCharacterSelect, new(MessageType: typeof(SelectPublicDataSetMessage), MessageTypeRaw: typeof(SelectPublicDataSetMessageData), Length: 16, SecurityKey: 0xDDCCBB17, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.ReceiveClientPublicDataFromServer, new(MessageType: typeof(PublicDataMessage), MessageTypeRaw: typeof(PublicDataMessageData), Length: 131104, SecurityKey: 0xDDCCBB18, DynamicSize: true, AllowedClients: ClientType.Server)},
        { SacredMsgType.ServerRequestsClientsPublicData, new(MessageType: typeof(PublicDataRequestMessage), MessageTypeRaw: typeof(PublicDataRequestMessageData), Length: 28, SecurityKey: 0xDDCCBB19, DynamicSize: false, AllowedClients: ClientType.Server)},
        { SacredMsgType.ServerListRequest, new(MessageType: typeof(RequestServerListMessage), MessageTypeRaw: typeof(RequestServerListMessageData), Length: 18, SecurityKey: 0xDDCCBB1A, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.SendServerInfo, new(MessageType: typeof(ServerInfoMessage), MessageTypeRaw: typeof(ServerInfoMessageData), Length: 130, SecurityKey: 0xDDCCBB1B, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.RemoveServer, new(MessageType: typeof(ServerInfoMessage), MessageTypeRaw: typeof(ServerInfoMessageData), Length: 130, SecurityKey: 0xDDCCBB1C, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ChannelListRequest, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 14, SecurityKey: 0xDDCCBB1D, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.SendChannelList, new(MessageType: typeof(ChannelListMessage), MessageTypeRaw: typeof(ChannelListMessageData), Length: 11664, SecurityKey: 0xDDCCBB1E, DynamicSize: true, AllowedClients: ClientType.None)},
        { SacredMsgType.ChannelJoinRequest, new(MessageType: typeof(JoinChannelMessage), MessageTypeRaw: typeof(JoinChannelMessageData), Length: 16, SecurityKey: 0xDDCCBB1F, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.UserJoinChannel, new(MessageType: typeof(JoinChannelMessage), MessageTypeRaw: typeof(JoinChannelMessageData), Length: 16, SecurityKey: 0xDDCCBB20, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ChannelLeaveRequest, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 14, SecurityKey: 0xDDCCBB21, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.OtherUserJoinedChannel, new(MessageType: typeof(ChannelUserMessage), MessageTypeRaw: typeof(ChannelUserMessageData), Length: 98, SecurityKey: 0xDDCCBB22, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.OtherUserLeftChannel, new(MessageType: typeof(ChannelUserMessage), MessageTypeRaw: typeof(ChannelUserMessageData), Length: 98, SecurityKey: 0xDDCCBB23, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ReceiveChatMessage, new(MessageType: typeof(ChatMessage), MessageTypeRaw: typeof(ChatMessageData), Length: 358, SecurityKey: 0xDDCCBB24, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.SendChatMessage, new(MessageType: typeof(ChatMessage), MessageTypeRaw: typeof(ChatMessageData), Length: 358, SecurityKey: 0xDDCCBB25, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ReceivePrivateChatMessage, new(MessageType: typeof(ChatMessage), MessageTypeRaw: typeof(ChatMessageData), Length: 358, SecurityKey: 0xDDCCBB26, DynamicSize: false, AllowedClients: ClientType.User | ClientType.Server)},
        { SacredMsgType.SendPrivateChatMessage, new(MessageType: typeof(ChatMessage), MessageTypeRaw: typeof(ChatMessageData), Length: 358, SecurityKey: 0xDDCCBB27, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.Kick, new(MessageType: typeof(KickMessage), MessageTypeRaw: typeof(KickMessageData), Length: 270, SecurityKey: 0xDDCCBB28, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.Alarm, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 0, SecurityKey: 0xDDCCBB29, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.FindUserRequest, new(MessageType: typeof(FindUserMessage), MessageTypeRaw: typeof(FindUserMessageData), Length: 94, SecurityKey: 0xDDCCBB2A, DynamicSize: false, AllowedClients: ClientType.User | ClientType.Server)},
        { SacredMsgType.FoundUser, new(MessageType: typeof(FriendInfoMessage), MessageTypeRaw: typeof(FriendInfoMessageData), Length: 274, SecurityKey: 0xDDCCBB2B, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ServerLoginResult, new(MessageType: typeof(ServerLoginInfoMessage), MessageTypeRaw: typeof(ServerLoginInfoMessageData), Length: 18, SecurityKey: 0xDDCCBB2C, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ClosedNetNewCharacter, new(MessageType: typeof(ClosedNetNewCharacterMessage), MessageTypeRaw: typeof(ClosedNetNewCharacterMessageData), Length: 18, SecurityKey: 0xDDCCBB2D, DynamicSize: false, AllowedClients: ClientType.User)},
        { SacredMsgType.Reserved1, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 0, SecurityKey: 0x0, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.Reserved2, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 0, SecurityKey: 0x0, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.Reserved3, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 0, SecurityKey: 0x0, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.Reserved4, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 0, SecurityKey: 0x0, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.Reserved5, new(MessageType: typeof(EmptyMessage), MessageTypeRaw: null, Length: 0, SecurityKey: 0x0, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.UserJoinedServer, new(MessageType: typeof(UserJoinedServerMessage), MessageTypeRaw: typeof(UserJoinedServerMessageData), Length: 22, SecurityKey: 0xDDCCBB2E, DynamicSize: false, AllowedClients: ClientType.Server)},
        { SacredMsgType.UserLeftServer, new(MessageType: typeof(UserLeftServerMessage), MessageTypeRaw: typeof(UserLeftServerMessageData), Length: 18, SecurityKey: 0xDDCCBB2F, DynamicSize: false, AllowedClients: ClientType.Server)},
        { SacredMsgType.UserKickedFromServer, new(MessageType: typeof(UserKickedFromServerMessage), MessageTypeRaw: typeof(UserKickedFromServerMessageData), Length: 278, SecurityKey: 0xDDCCBB30, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ClientImportantMessage, new(MessageType: typeof(ImportantMessage), MessageTypeRaw: typeof(ImportantMessageData), Length: 275, SecurityKey: 0xDDCCBB31, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ServerImportantMessage, new(MessageType: typeof(ImportantMessage), MessageTypeRaw: typeof(ImportantMessageData), Length: 275, SecurityKey: 0xDDCCBB32, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.ServerShutdown, new(MessageType: typeof(ServerShutdownMessage), MessageTypeRaw: typeof(ServerShutdownMessageData), Length: 270, SecurityKey: 0xDDCCBB33, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.AddFriend, new(MessageType: typeof(BuddyMessage), MessageTypeRaw: typeof(BuddyMessage), Length: 22, SecurityKey: 0xDDCCBB34, DynamicSize: false, AllowedClients: ClientType.User | ClientType.Server)},
        { SacredMsgType.RemoveFriend, new(MessageType: typeof(BuddyMessage), MessageTypeRaw: typeof(BuddyMessageData), Length: 22, SecurityKey: 0xDDCCBB35, DynamicSize: false, AllowedClients: ClientType.User | ClientType.Server)},
        { SacredMsgType.AddedToFriends, new(MessageType: typeof(BuddyAddedNotificationMessage), MessageTypeRaw: typeof(BuddyAddedNotificationMessageData), Length: 102, SecurityKey: 0xDDCCBB36, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.UpdateFriendStatus, new(MessageType: typeof(FriendInfoMessage), MessageTypeRaw: typeof(FriendInfoMessageData), Length: 274, SecurityKey: 0xDDCCBB37, DynamicSize: false, AllowedClients: ClientType.None)},
        { SacredMsgType.MessageOfTheDayRequest, new(MessageType: typeof(MotdRequestMessage), MessageTypeRaw: typeof(MotdRequestMessageData), Length: 144, SecurityKey: 0xDDCCBB38, DynamicSize: false, AllowedClients: ClientType.User | ClientType.Server)},
        { SacredMsgType.SendMessageOfTheDay, new(MessageType: typeof(MotdMessage), MessageTypeRaw: typeof(MotdMessageData), Length: 131218, SecurityKey: 0xDDCCBB39, DynamicSize: true, AllowedClients: ClientType.None)},
    };
}