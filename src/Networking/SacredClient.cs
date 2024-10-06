using System.Net;
using System.Net.Sockets;
using Sacred.Networking.Types;

namespace Sacred.Networking;

public class SacredClient
{
    public object _lock = new();
    public ClientType ClientType => connection.ClientType;
    public bool IsInChannel => Channel != -1;
    public uint ConnectionId { get; private set; }
    public IPEndPoint RemoteEndPoint => connection.RemoteEndPoint;
    public ServerInfo? ServerInfo { get; private set; }
    public string? clientName { get; private set; }
    public ProfileData Profile { get; private set; }
    public int SelectedBlock { get; set; }
    public int Channel { get; private set; }
    // TODO: Actually make Permanent Ids permanent when we implement persistent accounts...
    public int PermId => (int)ConnectionId;

    private SacredConnection connection;
    public SacredClient(Socket socket, uint connectionId)
    {
        connection = new SacredConnection(this, socket, connectionId);
        ConnectionId = connectionId;
        ServerInfo = null;
        Profile = ProfileData.CreateEmpty(PermId);
        Channel = -1;
    }

    public void Start()
    {
        Log.Info($"{GetPrintableName()} just connected");
        connection.Start();
    }

    public void Stop()
    {
        if(ClientType == ClientType.GameClient)
        {
            LobbyServer.UserLeftChannel(this);
        }
        else if(ClientType == ClientType.GameServer)
        {
            OnServerLogout();
        }

        connection.Stop();

        LobbyServer.RemoveClient(this);
    }

    public string GetPrintableName() => ClientType switch
    {
        ClientType.GameClient => $"{clientName}#{ConnectionId}",
        ClientType.GameServer => $"{ServerInfo?.Name}#{ConnectionId}",
        _ => $"{RemoteEndPoint}#{ConnectionId}",
    };

    public void SendPacket(SacredMsgType msgType, byte[] payload) => connection.EnqueuePacket(msgType, payload);

    public void ReceivePacket(SacredMsgType type, ReadOnlySpan<byte> payload)
    {
        var reader = new SpanReader(payload);

        switch (type)
        {
            case SacredMsgType.ClientLoginRequest:
            {
                var request = LoginRequest.Deserialize(payload);
                OnClientLoginRequest(request);
            }
            break;

            case SacredMsgType.ServerLoginRequest:
            {
                var serverInfo = ServerInfo.Deserialize(payload);
                OnServerLoginRequest(serverInfo);    
            }
            break;
            case SacredMsgType.ServerChangePublicInfo:
            {
                var serverInfo = ServerInfo.Deserialize(payload);
                OnServerChangePublicInfo(serverInfo);    
            }
            break;
            case SacredMsgType.ClientCharacterSelect:
            {
                var blockId = reader.ReadUInt16();
                OnClientCharacterSelect(blockId);
            }
            break;
            case SacredMsgType.ReceiveChatMessage:
            {
                var message = SacredChatMessage.Deserialize(payload);
                OnClientChatMessage(message);
            }
            break;
            case SacredMsgType.ReceivePublicData:
            {
                var data = PublicData.Deserialize(payload);
                OnReceivePublicData(data);
            }
            break;
            case SacredMsgType.PublicDataRequest:
            {
                var request = PublicDataRequest.Deserialize(payload);
                OnPublicDataRequest(request);
            }
            break;
            case SacredMsgType.ServerListRequest:
            {
                var request = ServerListRequest.Deserialize(payload);
                OnServerListRequest(request);
            }
            break;
            case SacredMsgType.ChannelJoinRequest:
            {
                var channel = (int)reader.ReadUInt16();
                OnChannelJoinRequest(channel);
            }
            break;            
            case SacredMsgType.ChannelLeaveRequest:
            {
                OnChannelLeaveRequest();
            }
            break;
            case SacredMsgType.MessageOfTheDayRequest:
            {
                var id = reader.ReadUInt16();
                OnMessageOfTheDayRequest(id);
            }
            break;
            case SacredMsgType.UserJoinedServer:
            {
                var permId = reader.ReadUInt32();
                var blockId = reader.ReadUInt16();

                OnUserJoinedServer(permId);
            }
            break;
            case SacredMsgType.ServerLogout:
            {
                OnServerLogout();
            }
            break;
            default:
            {
                if (Enum.IsDefined(type))
                    Log.Error($"Unimplemented packet {type}");
                else
                    Log.Error($"Unknown packet {(int)type}");
            }
            break;
        }

        SendLobbyResult(LobbyResults.Ok, type);
    }

    #region OnSacred
    public void OnChannelLeaveRequest()
    {
        Channel = -1;
        LobbyServer.UserLeftChannel(this);
    }

    public void OnServerLogout()
    {
        lock(_lock)
            LobbyServer.RemoveServer(ServerInfo!);
    }

    public void OnUserJoinedServer(uint permId)
    {
        
    }

    public void OnMessageOfTheDayRequest(ushort id)
    {
        string text = Config.Instance.MessageOfTheDay;
        var motd = new MessageOfTheDay(id, text);
        SendPacket(SacredMsgType.SendMessageOfTheDay, motd.Serialize());
    }

    public void OnChannelJoinRequest(int channel)
    {
        Log.Warning($"{GetPrintableName()} asked to join channel {channel}, but channels aren't implemented yet!");

        JoinChannel(0);
    }

    private void OnServerListRequest(ServerListRequest serverListRequest)
    {
        var id = serverListRequest.ChannelId;

        Log.Warning($"{GetPrintableName()} requested the server list for channel {id}, but channels aren't implemented yet!");

        SendServerList();
    }

    private void OnPublicDataRequest(PublicDataRequest request)
    {
        if(request.BlockId == Constants.ProfileBlockId)
        {          
            var client = LobbyServer.GetClientFromPermId(request.PermId);

            if(client != null)
            {
                ProfileData data;

                lock(client._lock)
                {
                    data = client.Profile;
                }            

                SendProfileData(client.Profile);
            }
            else
            {
                Log.Warning($"{GetPrintableName()} Requested public data of an invalid player: {request.PermId}");
                SendLobbyResult(LobbyResults.ErrorUserNotFound, SacredMsgType.PublicDataRequest);

            }
        }
        else if(request.BlockId <= 8)
        {
            Log.Error($"{GetPrintableName()} Character requests aren't implemented yet!");
            SendLobbyResult(LobbyResults.InternalError, SacredMsgType.PublicDataRequest);
        }
        else
        {
            Log.Error($"{GetPrintableName()} Requested public data with an invalid block {request}");
            SendLobbyResult(LobbyResults.InvalidBlockSelected, SacredMsgType.PublicDataRequest);
        }
    }


    private void OnReceivePublicData(PublicData publicData)
    {
        //The lobby received the client's profile data
        if(publicData.PermId == PermId && publicData.BlockId == Constants.ProfileBlockId)
        {
            lock(_lock)
            {
                Profile = publicData.ReadProfileData();
            }

            //Accept the changes
            SendLobbyResult(LobbyResults.ChangePublicDataSuccess, SacredMsgType.ReceivePublicData);

            //Update the data for all clients
            if(IsInChannel)
            {
                lock(_lock)
                    LobbyServer.BroadcastProfile(Profile);
            }
        }
    }


    public void OnClientLoginRequest(LoginRequest loginRequest)
    {
        if(Config.Instance.IsBanned(connection.RemoteEndPoint.Address, BanType.ClientOnly) == true)
        {
            Log.Info($"{GetPrintableName()} tried to log in as an user but was refused because it's banned");
            Stop();
            return;
        }

        Log.Trace($"{GetPrintableName()} logs in as user {loginRequest}");

        //We now know that a User is connecting
        connection.ClientType = ClientType.GameClient;

        clientName = loginRequest.Username;

        var loginResult = new LoginResult(
            Result: LobbyResults.Ok,
            Ip: connection.RemoteEndPoint.Address,
            PermId: PermId,
            Message: "Welcome!"
        );

        SendPacket(SacredMsgType.ClientLoginResult, loginResult.Serialize());
        SendLobbyResult(LobbyResults.Ok, SacredMsgType.ClientLoginRequest);

        Log.Info($"{GetPrintableName()} logged in as an user!");
    }

    private void OnServerLoginRequest(ServerInfo serverInfo)
    {
        if(Config.Instance.IsBanned(connection.RemoteEndPoint.Address, BanType.ServerOnly) == true)
        {
            Log.Info($"{GetPrintableName()} tried to log in as a server but was refused because it's banned");
            Stop();
            return;
        }  

        Log.Trace($"{GetPrintableName()} logs in as server");

        //We now know that a GameServer is connecting
        connection.ClientType = ClientType.GameServer;

        //Resolve the external IP of the server
        IPAddress externalIP;

        if (RemoteEndPoint.Address.IsInternal())
        {
            externalIP = Utils.GetExternalIp();
        }
        else
        {
            externalIP = RemoteEndPoint.Address;
        }

        //Accept the login
        SendPacket(SacredMsgType.ServerLoginResult, externalIP.GetAddressBytes());

        lock(_lock)
        {
            //Correct the server's info and save it
            ServerInfo = serverInfo with
            {
                ExternalIp = externalIP,
                ServerId = ConnectionId,
                ChannelId = 0
            };

            //Broadcast the new server to all clients
            LobbyServer.BroadcastServerInfo(ServerInfo);

            //Done
            Log.Info($"{GetPrintableName()} logged in as a server!");
            Log.Trace(ServerInfo.ToString());
        }

    }

    private void OnServerChangePublicInfo(ServerInfo newInfo)
    {
        lock(_lock)
        {
            ServerInfo = ServerInfo! with
            {
                Flags = newInfo.Flags,
                MaxPlayers = newInfo.MaxPlayers,
                CurrentPlayers = newInfo.CurrentPlayers
            };

            LobbyServer.BroadcastServerInfo(ServerInfo);
            
            Log.Info($"GameServer {GetPrintableName()} changed public info {ServerInfo.CurrentPlayers}/{ServerInfo.MaxPlayers} {ServerInfo.Flags}");
        }
    }

    public void OnClientCharacterSelect(ushort blockId)
    {
        
    }

    private void OnClientChatMessage(SacredChatMessage message)
    {
        //Ensure sender's parameters to prevent spoofing
        var msg = message with
        {
            SenderName = clientName ?? "<unknown>",
            SenderPermId = PermId
        };

        LobbyServer.BroadcastChatMessage(msg);

        Log.Info($"{GetPrintableName()} says: {msg.Message}");
    }
    #endregion

    public void SendChatMessage(string from, string message, int senderId) => SendChatMessage(new SacredChatMessage(from, senderId, PermId, message));

    public void SendChatMessage(SacredChatMessage message)
    {
        SendPacket(SacredMsgType.SendChatMessage, message.Serialize());
    }

    public void SendServerList()
    {
        var infos = LobbyServer.GetAllServerInfos();

        foreach (var info in infos)
        {
            SendPacket(SacredMsgType.SendServerInfo, info.Serialize());
        }
    }   

    public void JoinChannel(int channel)
    {
        // TODO: When fully implementing channels, we need to leave the one we're actually in
        if(Channel != channel)
        {
            Channel = channel;
            
            SendPacket(SacredMsgType.UserJoinChannel, BitConverter.GetBytes(channel));

            SendChannelChatMessage();

            LobbyServer.UserJoinedChannel(this);
        }
    }

    public void SendLobbyResult(LobbyResults result, SacredMsgType answeringTo)
    {
        SendPacket(SacredMsgType.LobbyResult, new LobbyResult(result, answeringTo).Serialize());
    }

    public void SendProfileData(ProfileData data)
    {
        var publicData = PublicData.FromProfileData(data.PermId, data);
        SendPacket(SacredMsgType.SendPublicData, publicData.Serialize());
    }

    public void OtherUserLeftChannel(int permId)
    {
        // The payload *should* be an UserJoinLeave
        // but since the name doesn't seem to be used, why the overhead of serializing the username?

        // var payload = new UserJoinLeave(permId, name).Serialize();
        var payload = BitConverter.GetBytes(permId);

        SendPacket(SacredMsgType.OtherUserLeftChannel, payload);
    }

    public void Kick()
    {
        Log.Info($"Kicking {GetPrintableName}");
        SendPacket(SacredMsgType.Kick, Array.Empty<byte>());
    }

    public void UpdateServerInfo(ServerInfo serverInfo) => SendPacket(SacredMsgType.ServerChangePublicInfo, serverInfo.Serialize());

    public void RemoveServer(ServerInfo serverInfo)
    {
        // I thought it was SacredMsgType.RemoveServer, but that doesn't work for some reason
        SendPacket(SacredMsgType.ServerLogout, serverInfo.Serialize());
    }

    public void SendImportantMessage(string message, bool showPopup = true)
    {
        var msg = new ImportantMessage(showPopup, message, PermId);

        SendPacket(SacredMsgType.ClientImportantMessage, msg.Serialize());
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