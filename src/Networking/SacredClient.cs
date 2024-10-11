using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
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
            OnChannelLeaveRequest();
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
    public void SendPacket<T>(SacredMsgType msgType, in T serializable) where T : ISerializable<T> => connection.EnqueuePacket(msgType, serializable.Serialize());

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
                var permId = reader.ReadInt32();
                var blockId = reader.ReadUInt16();

                OnUserJoinedServer(permId);
            }
            break;
            case SacredMsgType.UserLeftServer:
            {
                //NO-OP
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
        if(Channel != -1)
        {
            Channel = -1;
            LobbyServer.UserLeftChannel(this);
        }
    }

    public void OnServerLogout()
    {
        lock(_lock)
            LobbyServer.RemoveServer(ServerInfo!);
    }

    public void OnUserJoinedServer(int permId)
    {
        var client = LobbyServer.GetClientFromPermId(permId);

        if(client != null)
        {
            lock(client._lock)
            lock(_lock)
            {
                var accName = client.clientName;
                var charName = client.Profile.SelectedCharacter.Name;
                var gameName = ServerInfo!.Name;
                LobbyServer.BroadcastSystemMessage($"\\cFFFFFFFF - {accName}\\cFFFFFFFF joined {gameName}\\cFFFFFFFF with character {charName}");
            }

            Log.Info($"{client.GetPrintableName()} joined {GetPrintableName()}");
        }
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


        // Check for the name's validity
        clientName = loginRequest.Username;
        if(Regex.IsMatch(clientName, Config.Instance.AllowedUsernameRegex))
        {
            // We now know that a User is connecting
            connection.ClientType = ClientType.GameClient;

            var loginResult = new LoginResult(
                Result: LobbyResults.Ok,
                Ip: connection.RemoteEndPoint.Address,
                PermId: PermId,
                Message: "Welcome!"
            );

            SendPacket(SacredMsgType.ClientLoginResult, loginResult);
            SendLobbyResult(LobbyResults.Ok, SacredMsgType.ClientLoginRequest);

            Log.Info($"{GetPrintableName()} logged in as an user!");        
        }
        else
        {
            var loginResult = new LoginResult(
                Result: LobbyResults.InternalError,
                Ip: IPAddress.None,
                PermId: -1,
                Message: ""
            );

            SendPacket(SacredMsgType.ClientLoginResult, loginResult);

            SendImportantMessage("Your username is not allowed! Please choose a different one");

            Log.Info($"{GetPrintableName()} tried to login with an invalid username {clientName}");        
        }
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
        // Ignore the sender's parameters to prevent spoofing
        message.Deconstruct(out var _, out var _, out var destinationPermId, out var text);

        // Handle whisper messages
        if(text.StartsWith("/w"))
        {
            // Explained here: https://regex101.com/r/siaVkb/1
            var match = Regex.Match(text, @"\/w\s+(?<name>.*?)\s+(?<message>.*)");

            if(match.Success)
            {
                var name = match.Groups["name"].ValueSpan;
                var user = LobbyServer.GetUserFromPartialName(name);

                if(user != null)
                {
                    text = match.Groups["message"].Value;

                    user.SendChatMessage(@$"{clientName}\cAAAAAAAA whispers to you", PermId, @$"\cFFFFFFFF{text}");
                    SendSystemMessage(@$"\cAAAAAAAAYou whisper to {user.clientName}\cFFFFFFFF: {text}");
                }
                else
                {
                    SendSystemMessage("Player not found or multiple players found");
                }
            }
            else
            {
                SendSystemMessage("Syntax error! Usage: /w <player> <message>");
            }
        }
        else    //Normal message
        {
            var msg = message with
            {
                SenderName = clientName ?? "<unknown>",
                SenderPermId = PermId
            };

            LobbyServer.BroadcastChatMessage(msg);

            Log.Info($"{GetPrintableName()} says: {msg.Message}");
        }

    }
    #endregion

    public void SendChatMessage(string from, int senderId, string message) => SendChatMessage(new SacredChatMessage(from, senderId, PermId, message));
    public void SendSystemMessage(string message) => SendChatMessage(new SacredChatMessage("", 0, PermId, message));

    public void SendChatMessage(SacredChatMessage message)
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
        SendPacket(SacredMsgType.LobbyResult, new LobbyResult(result, answeringTo));
    }

    public void SendProfileData(ProfileData data)
    {
        var publicData = PublicData.FromProfileData(data.PermId, data);
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

    public void Kick()
    {
        Log.Info($"Kicking {GetPrintableName}");
        SendPacket(SacredMsgType.Kick, Array.Empty<byte>());
    }

    public void UpdateServerInfo(ServerInfo serverInfo) => SendPacket(SacredMsgType.ServerChangePublicInfo, serverInfo);

    public void RemoveServer(ServerInfo serverInfo)
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