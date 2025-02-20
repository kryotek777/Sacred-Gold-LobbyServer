using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Lobby.DB;
using Lobby.Types;
using Lobby.Types.Messages;
using Lobby.Types.Messages.Data;

namespace Lobby.Networking;

public class SacredClient
{
    public string ClientName { get; set; }
    public int Channel { get; set; }
    public ServerInfoMessage? ServerInfo { get; set; }
    public ProfileData? Profile { get; set; }
    public int SelectedCharacter { get; set; }
    public uint ConnectionId { get; private set; }
    public int PermId { get; set; }
    public bool IsAnonymous { get; set; }
    

    public ClientType ClientType
    {
        get => connection.ClientType;
        set => connection.ClientType = value;
    }
    public bool IsUser => ClientType == ClientType.User;
    public bool IsServer => ClientType == ClientType.Server;
    public bool IsInChannel => Channel != -1;
    public IPEndPoint RemoteEndPoint => connection.RemoteEndPoint;

    private readonly SacredConnection connection;

    public SacredClient(Socket socket, uint connectionId, CancellationToken parentToken)
    {
        connection = new SacredConnection(this, socket, connectionId, parentToken);
        ConnectionId = connectionId;
        ServerInfo = null;
        Profile = ProfileData.CreateEmpty(PermId);
        Channel = -1;
        ClientName = RemoteEndPoint.ToString();
        PermId = -1;
    }

    public void Start()
    {
        Log.Info($"{ClientName} just connected");
        connection.Start();
    }
    public void Stop()
    {
        connection.Stop();

        LobbyServer.RemoveClient(this);
    }
    public void SendUserLoginResult(LoginResultMessage msg) => SendPacket(SacredMsgType.ClientLoginResult, msg);
    public void SendServerLoginResult(IPAddress externalIp) => SendPacket(SacredMsgType.ServerLoginRequest, new ServerLoginInfoMessage(externalIp));
    public void SendChatMessage(string from, int senderId, string message) => SendChatMessage(new ChatMessage(from, senderId, PermId, message));
    public void SendSystemMessage(string message) => SendChatMessage(new ChatMessage("", 0, PermId, message));
    public void SendChatMessage(ChatMessage message)
    {
        SendPacket(SacredMsgType.SendChatMessage, message);
    }
    public void SendServerList(IEnumerable<ServerInfoMessage> infos)
    {
        foreach (var info in infos)
        {
            SendPacket(SacredMsgType.SendServerInfo, info);
        }
    }
    public void JoinChannel(int channel)
    {
        Channel = channel;

        var msg = new JoinChannelMessage(channel);
        SendPacket(SacredMsgType.UserJoinChannel, msg);
    }
    public void SendLobbyResult(LobbyResults result, SacredMsgType action)
    {
        SendPacket(SacredMsgType.LobbyResult, new ResultMessage(result, action));
    }
    public void SendProfileData(ProfileData data)
    {
        var publicData = PublicDataMessage.FromProfileData(data.PermId, data);
        SendPacket(SacredMsgType.SendPublicData, publicData);
    }
    public void OtherUserLeftChannel(int permId, string name)
    {
        var msg = new ChannelUserMessage(permId, name);
        SendPacket(SacredMsgType.OtherUserLeftChannel, msg);
    }
    public void Kick(string reason = "")
    {
        if(!string.IsNullOrWhiteSpace(reason))
            Log.Info($"Kicking {ClientName}: {reason}");
        else
            Log.Info($"Kicking {ClientName}");

        var msg = new KickMessage(reason);
        SendPacket(SacredMsgType.Kick, msg);
        Stop();
    }
    public void UpdateServerInfo(ServerInfoMessage serverInfo) => SendPacket(SacredMsgType.ServerChangePublicInfo, serverInfo);
    public void RemoveServer(ServerInfoMessage serverInfo)
    {
        // I thought it was SacredMsgType.RemoveServer, but that doesn't work for some reason
        SendPacket(SacredMsgType.ServerLogout, serverInfo);
    }
    public void SendImportantMessage(string message, bool showPopup = true)
    {
        var msg = new ImportantMessage(showPopup, message, PermId);
        SendPacket(SacredMsgType.ClientImportantMessage, msg);
    }
    public void SendMessageOfTheDay(ushort id, string text)
    {
        var msg = new MotdMessage(id, text);
        SendPacket(SacredMsgType.SendMessageOfTheDay, msg);
    }
    public void SendChannelChatMessage()
    {
        var message = Config.Instance.ChannelChatMessage;

        //Split the message into multiple lines to avoid cutoffs
        var lines = message.Split('\n');

        foreach (var line in lines)
        {
            SendChatMessage(
                from: string.Empty,     //No sender name
                message: line,          //Message
                senderId: 0             //From System (red text)
            );
        }
    }
    public void SendCharacter(CharacterPreview preview, int permId, short blockId)
    {
        // TODO: Find a cleaner way
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write(preview.Serialize());
        w.Write(0); // Length of the savegame

        var data = ms.ToArray();
        var pubData = new PublicDataMessage(permId, blockId, data.Length, 0, data.Length, data);
        
        SendPublicData(pubData);
    }
    public void SendSaveGame(SaveFile saveFile, int permId, short blockId)
    {
        // TODO: Find a cleaner way
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        var preview = saveFile.GetCharacterPreview();
        var saveData = saveFile.GetSaveData();
        var compr = Utils.ZLibCompress(saveData);

        w.Write(preview.Serialize());
        w.Write(compr.Length);
        w.Write(saveData.Length);
        w.Write(compr);

        var data = ms.ToArray();
        var pubData = new PublicDataMessage(permId, blockId, data.Length, 0, data.Length, data);
        
        SendPublicData(pubData);    
    }


    public void SendPublicData(PublicDataMessage data) => SendPacket(SacredMsgType.SendPublicData, data);
    public void SendChannelList(List<ChannelInfo> info) => SendPacket(SacredMsgType.SendChannelList, new ChannelListMessage(info));

    private void SendPacket<T>(SacredMsgType msgType, in T serializable) where T : ISerializable<T> => connection.EnqueuePacket(msgType, serializable.Serialize());
}