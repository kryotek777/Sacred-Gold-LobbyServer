using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Sacred.Networking.Types;

namespace Sacred.Networking;

public class SacredClient
{
    public ClientType ClientType { get; private set; }
    public uint ConnectionId { get; private set; }
    public IPEndPoint RemoteEndPoint => connection.RemoteEndPoint;
    public ServerInfo? ServerInfo { get; private set; }
    public string? clientName { get; private set; }
    public bool hasSelectedCharacter = false;
    public ProfileData profileData { get; private set; }

    private SacredConnection connection;
    private CancellationTokenSource cancellationTokenSource;
    private Task? readTask;
    private Task? writeTask;
    private BlockingCollection<TincatPacket> sendQueue;

    public SacredClient(SacredConnection connection, uint connectionId)
    {
        ArgumentNullException.ThrowIfNull(connection);

        this.connection = connection;
        ConnectionId = connectionId;
        cancellationTokenSource = new();
        readTask = null;
        writeTask = null;
        sendQueue = new(new ConcurrentQueue<TincatPacket>());
        ServerInfo = null;
    }

    public void Start()
    {
        LobbyServer.AddClient(this);

        readTask = Task.Factory.StartNew(
            ReadLoop,
            cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );

        writeTask = Task.Factory.StartNew(
            WriteLoop,
            cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    public void Stop()
    {
        LobbyServer.ForEachClient(x =>
        {
            if (x.ClientType == ClientType.GameClient && x.ConnectionId != ConnectionId)
                x.UserLeavedRoom(ConnectionId);
        });

        cancellationTokenSource.Cancel();

        LobbyServer.RemoveClient(this);
    }

    public void SendPacket(TincatPacket packet) => sendQueue.Add(packet);

    public string GetPrintableName()
    {
        if(ClientType == ClientType.GameClient)
        {
            return $"{clientName} #{ConnectionId} {RemoteEndPoint}";
        }
        else 
            return $"#{ConnectionId} {RemoteEndPoint}";
    }    

    private void ReadLoop()
    {
        while (!cancellationTokenSource.IsCancellationRequested && connection.IsConnected)
        {
            try
            {
                if (connection.TryReadPacket(out var packet, out var error))
                {
                    DispatchPacket(packet!);
                }
                else
                {
                    var message = error switch
                    {
                        PacketError.WrongMagic => $"Wrong Magic",
                        PacketError.WrongChecksum => $"Checksum Mismatch",
                        _ => $"Unknown"
                    };

                    Log.Error($"Error while reading packet from {GetPrintableName()}: {message}");
                }
            }
            catch (EndOfStreamException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"Unhandled error while reading packet from {GetPrintableName()}: {ex.Message}");
                Log.Trace(ex.ToString());
            }
        }

        Stop();
    }

    private void WriteLoop()
    {
        while (!cancellationTokenSource.IsCancellationRequested && connection.IsConnected)
        {
            try
            {
                var packet = sendQueue.Take();
                connection.SendPacket(packet);
            }
            catch (EndOfStreamException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"Unhandled error while writing packet to {GetPrintableName()}: {ex.Message}");
                Log.Trace(ex.ToString());
            }
        }

        Stop();
    }

    private void DispatchPacket(TincatPacket packet)
    {
        try
        {
            switch (packet.Header.Type)
            {
                case TincatMsgType.TIMESYNC:
                    OnTincatTimeSync(packet);
                    break;
                case TincatMsgType.CUSTOMDATA:
                    OnTincatCustomData(packet);
                    break;
                case TincatMsgType.LOGMEON:
                    OnTincatLogMeOn(packet);
                    break;
                case TincatMsgType.LOGMEOFF:
                    OnTincatLogMeOff(packet);
                    break;
                case TincatMsgType.LOGONACCEPTED:
                    OnTincatLogOnAccepted(packet);
                    break;
                case TincatMsgType.LOGOFFACCEPTED:
                    OnTincatLogOffAccepted(packet);
                    break;
                case TincatMsgType.STAYINGALIVE:
                    OnTincatStayingAlive(packet);
                    break;
                default:
                    Log.Error($"Unimplemented tincat message {(int)packet.Header.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Unhandled error while processing packet from {GetPrintableName()}: {ex.Message}");
            Log.Trace(ex.ToString());
        }
    }

    private void SendPacket(TincatHeader header, ReadOnlySpan<byte> payload)
    {
        SendPacket(new TincatPacket(header, payload.ToArray()));
    }

    private void SendPacket(TincatMsgType msgType, ReadOnlySpan<byte> payload)
    {
        SendPacket(MakePacket(msgType, payload));
    }

    public void SendPacket(SacredMsgType msgType, ReadOnlySpan<byte> payload)
    {
        SendPacket(MakePacket(msgType, payload));
    }

    private TincatPacket MakePacket(TincatMsgType msgType, ReadOnlySpan<byte> payload)
    {
        var header = new TincatHeader(
            TincatHeader.ServerId,
            ConnectionId,
            msgType,
            payload.Length,
            CRC32.Compute(payload)
        );

        return new TincatPacket(header, payload.ToArray());
    }

    private TincatPacket MakePacket(SacredMsgType msgType, ReadOnlySpan<byte> payload)
    {
        Span<byte> sacredData = stackalloc byte[SacredHeader.DataSize + payload.Length];

        //FIXME: Explain this stuff...
        var sacredHeader = new SacredHeader(msgType, payload.Length + SacredHeader.DataSize - 4);
        sacredHeader.Unknown1 = 0xDDCCBB00 + (uint)msgType;

        var headerData = sacredHeader.ToArray();

        for (int i = 0; i < headerData.Length; i++)
            sacredData[i] = headerData[i];

        for (int i = 0; i < payload.Length; i++)
            sacredData[i + headerData.Length] = payload[i];

        return MakePacket(TincatMsgType.CUSTOMDATA, sacredData);
    }

    #region OnTincat

    private void OnTincatTimeSync(TincatPacket packet)
    {
        Log.Error($"Unimplemented function {nameof(OnTincatTimeSync)}");
    }

    private void OnTincatCustomData(TincatPacket packet)
    {
        packet.Deconstruct(out var tincatHeader, out var tincatPayload);

        var sacredHeader = new SacredHeader(tincatPayload.AsSpan(0, SacredHeader.DataSize));
        var sacredPayload = tincatPayload.AsSpan(SacredHeader.DataSize);

        //Reply positively to every packet
        SendLobbyResult(LobbyResults.Ok, sacredHeader.Type1);

        switch (sacredHeader.Type1)
        {
            case SacredMsgType.ClientLoginRequest:
                OnClientLoginRequest(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.ServerLoginRequest:
                OnServerLoginRequest(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.ServerChangePublicInfo:
                OnServerChangePublicInfo(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.ClientCharacterSelect:
                OnClientCharacterSelect(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.ClientChatMessage:
                OnClientChatMessage(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.ClientLoginResult:
                OnAcceptClientLogin(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.LobbyResult:
                OnLobbyResult(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.AcceptServerLogin:
                OnServerStartInfo(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.ReceivePublicData:
                OnReceivePublicData(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.PublicDataRequest:
                OnPublicDataRequest(tincatHeader, sacredHeader, sacredPayload);
                break;
            case SacredMsgType.ServerListRequest:
                OnServerListRequest(tincatHeader, sacredHeader, sacredPayload);
                break;
            default:
                Log.Error($"Unimplemented Sacred message {(int)sacredHeader.Type1} from {GetPrintableName()}");
                Log.Trace(FormatPacket(tincatHeader, sacredHeader, sacredPayload));
                break;
        }


    }

    private void OnTincatLogMeOn(TincatPacket packet)
    {
        var logOnData = LogOn.Deserialize(packet.Payload);

        Log.Trace($"Got TincatLogMeOn from {GetPrintableName()}\n{logOnData}");

        if (
            logOnData.Magic == LogOn.LogOnMagic &&
            logOnData.ConnectionId == LogOn.LogOnConnId &&
            logOnData.Username == "user" &&
            logOnData.Password == "passwor"
        )
        {
            var response = new LogOn(ConnectionId);
            SendPacket(TincatMsgType.LOGONACCEPTED, response.Serialize());

            Log.Trace($"{GetPrintableName()} Logged at tincat level\n{response}");
        }
        else
        {
            Log.Error($"Wrong connection Id during Tincat LogMeOn from {GetPrintableName()}");

            var response = new LogOn(uint.MaxValue);
        }
    }

    private void OnTincatLogMeOff(TincatPacket packet)
    {
        Log.Error($"Unimplemented function {nameof(OnTincatLogMeOff)}");
    }

    private void OnTincatLogOnAccepted(TincatPacket packet)
    {
        Log.Error($"Unimplemented function {nameof(OnTincatLogOnAccepted)}");
    }

    private void OnTincatLogOffAccepted(TincatPacket packet)
    {
        Log.Error($"Unimplemented function {nameof(OnTincatLogOffAccepted)}");
    }

    private void OnTincatStayingAlive(TincatPacket packet)
    {

    }

    #endregion

    #region OnSacred
    private void OnServerListRequest(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        var serverListRequest = ServerListRequest.Deserialize(payload);

        var id = serverListRequest.ChannelId;

        Log.Trace($"{GetPrintableName()} requested the server list for channel {id}, but channels aren't implemented yet");

        SendServerList();
    }

    private void OnPublicDataRequest(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        var request = PublicDataRequest.Deserialize(payload);

        if(request.PermId == (int)ConnectionId && request.BlockId == 10)
        {          
            if(profileData != null)
            {
                Log.Trace($"{GetPrintableName()} Requested their public data");

                SendProfileData((int)ConnectionId, profileData);
            }
        }
        else if(request.BlockId <= 8)
        {
            Log.Error($"{GetPrintableName()} Character requests aren't implemented yet!");
        }
        else
        {
            Log.Error($"{GetPrintableName()} Requested public data with an invalid block {request}");
        }
    }


    private void OnReceivePublicData(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        var pubData = PublicData.Deserialize(payload);

        //The lobby received the client's profile data
        if(pubData.PermId == (int)ConnectionId && pubData.BlockId == Constants.ProfileBlockId)
        {
            profileData = pubData.ReadProfileData();

            //Accept the changes
            SendLobbyResult(LobbyResults.ChangePublicDataSuccess, SacredMsgType.ReceivePublicData);

            //Update the data for all clients
            LobbyServer.ForEachClient(x => x.SendProfileData((int)ConnectionId, profileData));
        }
    }

    private void OnClientLoginRequest(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        if(LobbyServer.BanList?.IsBanned(connection.RemoteEndPoint.Address, BanType.ClientOnly) == true)
        {
            Stop();
            return;
        }   

        ClientType = ClientType.GameClient;

        var loginRequest = LoginRequest.Deserialize(payload);

        clientName = loginRequest.Username;

        var loginResult = new LoginResult(
            Result: LobbyResults.Ok,
            Ip: connection.RemoteEndPoint.Address,
            PermId: (int)ConnectionId,
            Message: "Welcome!"
        );

        SendPacket(SacredMsgType.ClientLoginResult, loginResult.Serialize());

        Log.Info($"Client logged in:\n{loginRequest}");
        Log.Trace($"Answering with:\n{loginResult}");
    }

    private void OnServerLoginRequest(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        if(LobbyServer.BanList?.IsBanned(connection.RemoteEndPoint.Address, BanType.ServerOnly) == true)
        {
            Stop();
            return;
        }  
        //We now know that a GameServer is connecting
        ClientType = ClientType.GameServer;

        //Save the server's information
        ServerInfo = ServerInfo.Deserialize(payload);

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

        //Correct the server's info
        ServerInfo = ServerInfo with
        {
            ExternalIp = externalIP,
            ServerId = ConnectionId,
            ChannelId = 0
        };

        //Accept the login
        SendPacket(SacredMsgType.AcceptServerLogin, externalIP.GetAddressBytes());

        //Broadcast the new server to all clients
        var packet = MakePacket(SacredMsgType.UpdateServerInfo, ServerInfo.Serialize());
        LobbyServer.SendPacketToAllGameClients(packet);

        //Done
        Log.Info($"{GetPrintableName()} connected as a GameServer with name '{ServerInfo.Name}'");
        Log.Trace(ServerInfo.ToString());
    }

    private void OnServerChangePublicInfo(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        var newInfo = ServerInfo.Deserialize(payload);

        ServerInfo = ServerInfo! with
        {
            Flags = newInfo.Flags,
            MaxPlayers = newInfo.MaxPlayers,
            CurrentPlayers = newInfo.CurrentPlayers
        };

        Log.Info($"GameServer {GetPrintableName()} changed public info");

        var packet = MakePacket(SacredMsgType.UpdateServerInfo, ServerInfo.Serialize());
        LobbyServer.SendPacketToAllGameClients(packet);

    }
    private void OnClientCharacterSelect(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        //Immediately join Room #0
        //Rooms aren't implemented yet, but if we force the right answer the client will happily join
        JoinRoom(0);

        //Send the MOTD
        SendMotd();

        hasSelectedCharacter = true;

        LobbyServer.ForEachClient(x =>
        {
            if (x.ClientType == ClientType.GameClient && x.hasSelectedCharacter && x.ConnectionId != ConnectionId)
            {
                x.UserJoinedRoom((int)ConnectionId, clientName);
                UserJoinedRoom((int)x.ConnectionId, x.clientName);

                x.SendProfileData((int)ConnectionId, profileData);
                SendProfileData((int)x.ConnectionId, x.profileData);
            }
        });
    }

    private void OnClientChatMessage(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        //Ensure sender's parameters to prevent spoofing
        var msg = SacredChatMessage.Deserialize(payload) with
        {
            SenderName = clientName ?? "<unknown>",
            SenderPermId = (int)ConnectionId
        };

        LobbyServer.SendPacketToAllGameClients(MakePacket(SacredMsgType.SendSystemMessage, msg.Serialize()));

        Log.Trace($"ChatMsg from {GetPrintableName()}: {msg.Message}");
    }
    private void OnAcceptClientLogin(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        Log.Error($"Unimplemented function {nameof(OnAcceptClientLogin)}");
    }
    private void OnLobbyResult(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        Log.Error($"Unimplemented function {nameof(OnLobbyResult)}");
    }
    private void OnServerStartInfo(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        Log.Error($"Unimplemented function {nameof(OnServerStartInfo)}");
    }

    #endregion

    public void SendChatMessage(string from, string message, int senderId) => SendChatMessage(new SacredChatMessage(from, senderId, (int)ConnectionId, message));

    public void SendChatMessage(SacredChatMessage message)
    {
        SendPacket(MakePacket(SacredMsgType.SendSystemMessage, message.Serialize()));
    }

    public void SendServerList()
    {
        var infos = LobbyServer.GetAllServerInfos();

        foreach (var info in infos)
        {
            SendPacket(SacredMsgType.UpdateServerInfo, info.Serialize());
        }
    }   

    public void JoinRoom(int roomNumber)
    {
        SendPacket(SacredMsgType.ClientJoinRoom, BitConverter.GetBytes(roomNumber));
    }

    public void SendLobbyResult(LobbyResults result, SacredMsgType answeringTo)
    {
        SendPacket(SacredMsgType.LobbyResult, new LobbyResult(result, answeringTo).Serialize());
    }

    public void SendProfileData(int permId, ProfileData data)
    {
        var publicData = PublicData.FromProfileData(permId, data);
        SendPacket(SacredMsgType.SendPublicData, publicData.Serialize());
    }

    public void UserJoinedRoom(int permId, string name)
    {
        var data = new UserJoinLeave(permId, name);

        SendPacket(SacredMsgType.OtherClientJoinedLobby, data.Serialize());
    }

    public void UserLeavedRoom(uint connId)
    {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);

        w.Write(connId);

        SendPacket(SacredMsgType.OtherClientLeavedLobby, ms.ToArray());
    }

    public void Kick()
    {
        SendPacket(SacredMsgType.Kick, ReadOnlySpan<byte>.Empty);
    }

    private void SendMotd()
    {
        var motd = Config.Instance.MOTD;

        if (motd == null)
            return;

        foreach (var line in motd)
        {
            if (line == null)
                continue;

            SendChatMessage(
                from: string.Empty, //Red Text
                message: line,      //MOTD line
                senderId: 0        //From System
            );
        }
    }

    private string FormatPacket(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---Tincat Header---");
        sb.AppendLine(tincatHeader.ToString());
        sb.AppendLine("---Sacred Header---");
        sb.AppendLine(sacredHeader.ToString());
        sb.AppendLine("---Payload Data---");
        Utils.FormatBytes(payload, sb);

        return sb.ToString();
    }
}