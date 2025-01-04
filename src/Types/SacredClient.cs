using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Lobby.Types;
using Lobby.Types.Messages;
using Lobby.Types.Messages.Data;

namespace Lobby.Networking;

public class SacredClient
{
    public ClientType ClientType 
    {
        get => connection.ClientType;
        set => connection.ClientType = value;
    }
    public bool IsInChannel => Channel != -1;
    public uint ConnectionId { get; private set; }
    public IPEndPoint RemoteEndPoint => connection.RemoteEndPoint;
    public ServerInfoMessage? ServerInfo { get; set; }
    public string ClientName { get; set; }
    public ProfileData Profile { get; set; }
    public int SelectedBlock { get; set; }
    public int Channel { get; set; }
    // TODO: Actually make Permanent Ids permanent when we implement persistent accounts...
    public int PermId => (int)ConnectionId;

    public bool IsUser => ClientType == ClientType.User;
    public bool IsServer => ClientType == ClientType.Server;

    private SacredConnection connection;
    public SacredClient(Socket socket, uint connectionId, CancellationToken parentToken)
    {
        connection = new SacredConnection(this, socket, connectionId, parentToken);
        ConnectionId = connectionId;
        ServerInfo = null;
        Profile = ProfileData.CreateEmpty(PermId);
        Channel = -1;
        ClientName = "<unknown>";
    }

    public void Start()
    {
        Log.Info($"{GetPrintableName()} just connected");
        connection.Start();
    }

    public void Stop()
    {
        connection.Stop();

        LobbyServer.RemoveClient(this);
    }

    public string GetPrintableName() => ClientType switch
    {
        ClientType.User => $"{ClientName}#{ConnectionId}",
        ClientType.Server => $"{ServerInfo?.Name}#{ConnectionId}",
        _ => $"{RemoteEndPoint}#{ConnectionId}",
    };

    public void SendPacket(SacredMsgType msgType, byte[] payload) => connection.EnqueuePacket(msgType, payload);
    public void SendPacket<T>(SacredMsgType msgType, in T serializable) where T : ISerializable<T> => connection.EnqueuePacket(msgType, serializable.Serialize());

    public void SendChatMessage(string from, int senderId, string message) => SendChatMessage(new ChatMessage(from, senderId, PermId, message));
    public void SendSystemMessage(string message) => SendChatMessage(new ChatMessage("", 0, PermId, message));

    public void SendChatMessage(ChatMessage message)
    {
        SendPacket(SacredMsgType.SendChatMessage, message);
    }

    public void SendServerList()
    {
        var infos = LobbyServer.GetAllServerInfos();

        foreach (var info in infos)
        {
            SendPacket(SacredMsgType.SendServerInfo, info);
        }
    }

    public void JoinChannel(int channel)
    {
        // TODO: When fully implementing channels, we need to leave the one we're actually in
        if (Channel != channel)
        {
            Channel = channel;

            SendPacket(SacredMsgType.UserJoinChannel, BitConverter.GetBytes(channel));

            SendChannelChatMessage();

            LobbyServer.UserJoinedChannel(this);
        }
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

    public void OtherUserLeftChannel(int permId)
    {
        // The payload *should* be an UserJoinLeave
        // but since the name doesn't seem to be used, why the overhead of serializing the username?

        // var payload = new UserJoinLeave(permId, name).Serialize();
        var payload = BitConverter.GetBytes(permId);

        SendPacket(SacredMsgType.OtherUserLeftChannel, payload);
    }

    public void Kick(string reason = "")
    {
        Log.Info($"Kicking {GetPrintableName()}");

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

    private void SendChannelChatMessage()
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
}