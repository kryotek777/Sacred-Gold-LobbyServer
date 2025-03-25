using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Lobby.DB;
using Lobby.Networking;
using Lobby.Types;
using Lobby.Types.Messages;
using Lobby.Types.Messages.Data;
namespace Lobby;

internal static partial class LobbyServer
{
    private static readonly List<ServerInfoMessage> separators = new();
    private static readonly CancellationTokenSource cancellationTokenSource = new();
    private static uint connectionIdCounter = 0;

    private static readonly ConcurrentDictionary<uint, SacredClient> ClientDictionary = new();
    public static readonly IEnumerable<SacredClient> Clients = ClientDictionary.Select(x => x.Value);
    public static readonly IEnumerable<SacredClient> Users = Clients.Where(c => c.ClientType == ClientType.User);
    public static readonly IEnumerable<SacredClient> Servers = Clients.Where(c => c.ClientType == ClientType.Server);
    private static readonly Channel<SacredPacket> ReceivedPackets = Channel.CreateUnbounded<SacredPacket>();
    private static readonly ConcurrentQueue<ChatMessage> ChatHistory = new();
    private static List<ChannelInfo> ChannelList => Config.Instance.Channels;

    public static Task Run()
    {
        LoadConfig();
        Database.Load();

        List<Task> tasks =
        [
            AcceptLoopAsync(cancellationTokenSource.Token),
            ProcessLoopAsync(cancellationTokenSource.Token),
            Utils.RunTask(InteractiveConsole.Run, cancellationTokenSource.Token),
        ];

        return Task.WhenAll(tasks);
    }

    public static void Stop()
    {
        Log.Info("Exiting...");

        cancellationTokenSource.Cancel();

        Database.Close();
    }

    public static void ReceivePacket(SacredPacket packet)
    {
        var written = ReceivedPackets.Writer.TryWrite(packet);
        
        if(!written && !cancellationTokenSource.Token.IsCancellationRequested)
        {
            Log.Error("Failed to write packet to the processing queue!");
        }
    }

    public static void RemoveClient(SacredClient client)
    {
        if (client.IsUser)
            OnChannelLeaveRequest(client);

        if (client.IsServer)
            OnServerLogout(client, client.ServerInfo!);

        ClientDictionary.Remove(client.ConnectionId, out _);
        Log.Info($"Client removed {client.ClientName}");
    }

    public static SacredClient? GetClientFromPermId(int permId) => Clients.FirstOrDefault(x => permId == x.PermId);

    /// <summary>
    /// Retrieves a user from a partial name
    /// Returns <see cref="null"/> if no matches are available or if there are multiple amiguous matches
    /// </summary>
    /// <param name="name">The partial name of the user</param>
    /// <returns>The matching user or <see cref="null"/></returns>
    public static SacredClient? GetUserFromPartialName(ReadOnlySpan<char> name)
    {
        SacredClient? value = null;

        foreach (var user in Users)
        {
            if (user.IsInChannel && user.ClientName!.AsSpan().Contains(name, StringComparison.InvariantCultureIgnoreCase))
            {
                // If we haven't got another match, save the result
                if (value == null)
                    value = user;
                else // If we did, we're not returning anything
                    return null;
            }
        }

        return value;
    }

    public static void BroadcastProfile(ProfileData profile)
    {
        foreach (var user in Users)
        {
            if (user.IsInChannel)
                user.SendProfileData(profile);
        }
    }

    public static void BroadcastServerInfo(ServerInfoMessage info)
    {
        foreach (var user in Users)
        {
            if (user.IsInChannel)
                user.UpdateServerInfo(info);
        }
    }

    public static void RemoveServer(ServerInfoMessage info)
    {
        foreach (var user in Users)
        {
            if (user.IsInChannel)
                user.RemoveServer(info);
        }
    }

    /// <summary>
    /// Sends a chat message to everyone but the sender
    /// </summary>
    /// <param name="chatMessage">The message to broadcast</param>
    public static void BroadcastChatMessage(ChatMessage chatMessage)
    {
        var maxHistory = Config.Instance.ChatHistoryLimit;
        if (maxHistory > 0)
        {
            if (ChatHistory.Count == maxHistory)
                ChatHistory.TryDequeue(out _);

            ChatHistory.Enqueue(chatMessage);
        }

        foreach (var user in Users)
        {
            if (user.IsInChannel && user.PermId != chatMessage.SenderPermId)
                user.SendChatMessage(chatMessage with { DestinationPermId = user.PermId });
        }
    }

    public static void BroadcastSystemMessage(string message)
    {
        foreach (var user in Users)
        {
            if (user.IsInChannel)
            {
                user.SendSystemMessage(message);
            }
        }
    }

    private static void LoadConfig()
    {
        // If the config fails to load for some reason, sane defaults are used
        bool loadedDefaults = Config.Load(out var error);

        // Initialize the log
        Log.Initialize(Config.Instance.LogLevel, Config.Instance.LogPath);

        // Show the previous error
        if (!loadedDefaults)
            Log.Error(error!);

        BuildSeparators();

        Log.Info("Config loaded!");
    }

    private static async Task AcceptLoopAsync(CancellationToken token)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, Config.Instance.Port);
            listener.Start();

            Log.Info("Started accepting clients");

            while (!token.IsCancellationRequested)
            {
                var socket = await listener.AcceptSocketAsync(token);
                var endPoint = (socket.RemoteEndPoint as IPEndPoint)!;
                var remoteIp = endPoint.Address;

                if (Config.Instance.IsBanned(remoteIp, BanType.Full))
                {
                    Log.Info($"Connection refused from {remoteIp} because IP is banned");
                    socket.Close();
                    socket.Dispose();
                }
                else
                {
                    var connId = ++connectionIdCounter;
                    var client = new SacredClient(socket, connId, token);
                    ClientDictionary[connId] = client;
                    client.Start();
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    private static void BuildSeparators()
    {
        separators.Clear();
        separators.AddRange(Config.Instance.ServerSeparators.Select((name, i) => new ServerInfoMessage(
            Name: name,
            LocalIp: IPAddress.None,
            ExternalIp: IPAddress.None,
            Port: 0,
            PlayerCount: 0,
            MaxPlayers: 0,
            Flags: 0,
            ServerId: uint.MaxValue - (uint)i, // Give an Id that will never be used in practice
            NetworkVersion: 0,
            ClientGameVersion: 0,
            ChannelId: 0
        )));
    }

    private static async Task ProcessLoopAsync(CancellationToken token)
    {
        await Task.Factory.StartNew(async () =>
        {
            try
            {
                var reader = ReceivedPackets.Reader;

                while (!token.IsCancellationRequested)
                {
                    SacredClient? sender = null;

                    try
                    {
                        var packet = await reader.ReadAsync(token);
                        packet.Deconstruct(out sender, out var type, out var payload);
                        ProcessPacket(packet);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error while processing packet! Sender: {sender?.ClientName ?? "null"} Exception: {ex}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Trace("Processing thread stopped");
            }
            catch (Exception ex)
            {
                Log.Error($"Processing thread crashed! {ex}");
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

    }

    private static void ProcessPacket(SacredPacket packet)
    {
        packet.Deconstruct(out var sender, out var type, out var payload);
        (LobbyResults code, string? message) result;

        switch (type)
        {
            case SacredMsgType.ClientRegistrationRequest:
                {

                    var data = RegistrationMessage.Deserialize(payload);
                    result = OnClientRegistrationRequest(sender, data);
                    break;
                }
            case SacredMsgType.ClientLoginRequest:
                {
                    var data = LoginMessage.Deserialize(payload);
                    result = OnClientLoginRequest(sender, data);
                    break;
                }
            case SacredMsgType.PrivateInfoRequest:
                {
                    result = OnPrivateInfoRequest(sender);
                    break;
                }
            case SacredMsgType.ReceivePrivateInfo:
                {
                    var data = PrivateInfoMessage.Deserialize(payload);
                    result = OnReceivePrivateInfo(sender, data);
                    break;
                }
            case SacredMsgType.PublicInfoRequest:
                {
                    var data = PublicInfoRequestMessage.Deserialize(payload);
                    result = OnPublicInfoRequest(sender, data);
                    break;
                }
            case SacredMsgType.ReceivePublicInfo:
                {
                    var data = PublicInfoMessage.Deserialize(payload);
                    result = OnReceivePublicInfo(sender, data);
                    break;
                }
            case SacredMsgType.ServerRequestsClientsPublicData:
            case SacredMsgType.PublicDataRequest:
                {
                    var data = PublicDataRequestMessage.Deserialize(payload);
                    result = OnPublicDataRequest(sender, data);
                    break;
                }
            case SacredMsgType.ReceiveClientPublicDataFromServer:
            case SacredMsgType.ReceivePublicData:
                {
                    var data = PublicDataMessage.Deserialize(payload);
                    result = OnReceivePublicData(sender, data);
                    break;
                }
            case SacredMsgType.ServerLoginRequest:
                {
                    var data = ServerInfoMessage.Deserialize(payload);
                    result = OnServerLoginRequest(sender, data);
                    break;
                }
            case SacredMsgType.ServerChangePublicInfo:
                {
                    var data = ServerInfoMessage.Deserialize(payload);
                    result = OnServerChangePublicInfo(sender, data);
                    break;
                }
            case SacredMsgType.ServerLogout:
                {
                    var data = ServerInfoMessage.Deserialize(payload);
                    result = OnServerLogout(sender, data);
                    break;
                }
            case SacredMsgType.ClientCharacterSelect:
                {
                    var data = SelectPublicDataSetMessage.Deserialize(payload);
                    result = OnClientCharacterSelect(sender, data);
                    break;
                }
            case SacredMsgType.ServerListRequest:
                {
                    var data = RequestServerListMessage.Deserialize(payload);
                    result = OnServerListRequest(sender, data);
                    break;
                }
            case SacredMsgType.ChannelListRequest:
                {
                    result = OnChannelListRequest(sender);
                    break;
                }
            case SacredMsgType.ChannelJoinRequest:
                {
                    var data = JoinChannelMessage.Deserialize(payload);
                    result = OnChannelJoinRequest(sender, data);
                    break;
                }
            case SacredMsgType.ChannelLeaveRequest:
                {
                    result = OnChannelLeaveRequest(sender);
                    break;
                }
            case SacredMsgType.ReceivePrivateChatMessage:
            case SacredMsgType.ReceiveChatMessage:
                {
                    var data = ChatMessage.Deserialize(payload);
                    result = OnReceiveChatMessage(sender, data);
                    break;
                }
            case SacredMsgType.Alarm:
                {
                    result = OnAlarm(sender);
                    break;
                }
            case SacredMsgType.FindUserRequest:
                {
                    var data = FindUserMessage.Deserialize(payload);
                    result = OnFindUserRequest(sender, data);
                    break;
                }
            case SacredMsgType.ClosedNetNewCharacter:
                {
                    var data = ClosedNetNewCharacterMessage.Deserialize(payload);
                    result = OnClosedNetNewCharacter(sender, data);
                    break;
                }
            case SacredMsgType.UserJoinedServer:
                {
                    var data = UserJoinedServerMessage.Deserialize(payload);
                    result = OnUserJoinedServer(sender, data);
                    break;
                }
            case SacredMsgType.UserLeftServer:
                {
                    var data = UserLeftServerMessage.Deserialize(payload);
                    result = OnUserLeftServer(sender, data);
                    break;
                }
            case SacredMsgType.AddFriend:
                {
                    var data = BuddyMessage.Deserialize(payload);
                    result = OnAddFriend(sender, data);
                    break;
                }
            case SacredMsgType.RemoveFriend:
                {
                    var data = BuddyMessage.Deserialize(payload);
                    result = OnRemoveFriend(sender, data);
                    break;
                }
            case SacredMsgType.MessageOfTheDayRequest:
                {
                    var data = MotdRequestMessage.Deserialize(payload);
                    result = OnMessageOfTheDayRequest(sender, data);
                    break;
                }
            default:
                {
                    if (Enum.IsDefined(type))
                    {
                        Log.Error($"Packet with number {(int)type} isn't defined. Is the client modded?");
                    }
                    else
                    {
                        Log.Error($"Unimplemented packet {type}");
                    }

                    result = (LobbyResults.InternalError, "Your client sent an unknown packet, please report a bug!");
                    break;
                }
        }

        sender.SendLobbyResult(result.code, type);

        if (!string.IsNullOrWhiteSpace(result.message))
        {
            sender.SendImportantMessage(result.message);
        }
    }

    private static (LobbyResults code, string? message) OnClientRegistrationRequest(SacredClient sender, RegistrationMessage data)
    {
        if (!Config.Instance.StorePersistentData)
            return (LobbyResults.InternalError, "Registration is not supported. You can log in with any username and password you wish");

        data.Deconstruct(out var username, out var password, out _, out _, out _, out _, out _);

        if (Database.TryGetAccount(username, out _))
        {
            return (LobbyResults.ErrorUsernameExists, null);
        }

        Database.CreateAccount(username, password);

        return (LobbyResults.Ok, null);

    }
    private static (LobbyResults code, string? message) OnClientLoginRequest(SacredClient sender, LoginMessage loginRequest)
    {
        if (Config.Instance.IsBanned(sender.RemoteEndPoint.Address, BanType.ClientOnly))
        {
            Log.Info($"{sender.ClientName} tried to log in as a user but was refused because it's banned");
            sender.Stop();
            return (LobbyResults.ErrorUserBanned, null);
        }

        if (Config.Instance.StorePersistentData)
        {
            if (Database.TryLogin(loginRequest.Username, loginRequest.Password, out var account))
            {
                sender.Profile = account.GetProfileData();

                sender.ClientType = ClientType.User;
                sender.ClientName = account.Username;
                sender.PermId = account.PermId;
                sender.IsAnonymous = false;

                var loginResult = new LoginResultMessage(
                    Result: LobbyResults.Ok,
                    Ip: sender.RemoteEndPoint.Address,
                    PermId: sender.PermId,
                    Message: "Welcome!"
                );

                sender.SendUserLoginResult(loginResult);
                sender.SendProfileData(sender.Profile);

                Log.Info($"{sender.ClientName} logged in as a user!");

                return (LobbyResults.Ok, null);
            }
            else
            {
                if (account != null)
                    return (LobbyResults.InvalidPassword, null);

                if (!Config.Instance.AllowAnonymousLogin)
                    return (LobbyResults.ErrorUserNotFound, null);

            }
        }

        if (Config.Instance.AllowAnonymousLogin && Regex.IsMatch(loginRequest.Username, Config.Instance.AllowedUsernameRegex))
        {
            sender.ClientType = ClientType.User;
            sender.ClientName = loginRequest.Username;
            sender.PermId = (int)sender.ConnectionId + 1000000;
            sender.IsAnonymous = true;

            var loginResult = new LoginResultMessage(
                Result: LobbyResults.Ok,
                Ip: sender.RemoteEndPoint.Address,
                PermId: sender.PermId,
                Message: "Welcome!"
            );

            sender.SendUserLoginResult(loginResult);

            Log.Info($"{sender.ClientName} logged in as a user!");

            return (LobbyResults.Ok, null);
        }
        else
        {
            Log.Info($"{sender.ClientName} tried to login with an invalid username {loginRequest.Username}");

            return (LobbyResults.InternalError, "Your username is not allowed! Please choose a different one");
        }
    }

    private static (LobbyResults code, string? message) OnPrivateInfoRequest(SacredClient sender)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnReceivePrivateInfo(SacredClient sender, PrivateInfoMessage data)
    {
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnPublicInfoRequest(SacredClient sender, PublicInfoRequestMessage data)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnReceivePublicInfo(SacredClient sender, PublicInfoMessage data)
    {
        return (LobbyResults.InternalError, null);
    }
    private static (LobbyResults code, string? message) OnPublicDataRequest(SacredClient sender, PublicDataRequestMessage request)
    {
        // Anyone can ask for the cheater counter block
        if (request.BlockId == Constants.CheaterCounterBlockId)
        {
            //TODO: Properly reverse this?
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            var marker = 0x20202020;
            w.Write(marker);
            w.Write(0);
            w.Write(new byte[6 * 4]);
            var data = ms.ToArray();

            var msg = new PublicDataMessage(request.PermId, 0, data.Length, 0, data.Length, data);
            sender.SendPublicData(msg);

            return (LobbyResults.Ok, null);
        }
        // Anyone can ask for a player's profile
        else if (request.BlockId == Constants.ProfileBlockId)
        {
            var client = GetClientFromPermId(request.PermId);

            if (client != null)
            {
                sender.SendProfileData(client.Profile!);
                return (LobbyResults.Ok, null);
            }
            else
            {
                Log.Warning($"{sender.ClientName} Requested public data of an invalid player: {request.PermId}");
                return (LobbyResults.ErrorUserNotFound, null);
            }
        }
        // A character save is requested, investigate more
        else if (request.BlockId <= 8)
        {
            // Closed net servers can request full savegames
            // The player can request it's own characters
            // Else, it's someone else and needs to be booted
            if (!sender.IsServer && request.PermId != sender.PermId)
            {
                Log.Warning($"{sender.ClientName} tried to ask another player's data!");

                return (LobbyResults.ErrorNotAGame, null);
            }

            if (sender.IsAnonymous)
            {
                //HACK: Return an error only for the first blockId to not spam the user
                if (request.BlockId == 1)
                {
                    sender.Kick("Anonymous accounts cannot play closednet, please register a new account to play!");
                }

                return (LobbyResults.Ok, null);
            }

            var saveData = Database.GetSaveFile(request.PermId, request.BlockId);

            if (saveData != null)
            {
                // We should send the whole savegame too
                if (sender.IsServer)
                {
                    sender.SendSaveGame(saveData, request.PermId, request.BlockId);
                }
                else // Just the preview for the character selector
                {
                    var preview = saveData.GetCharacterPreview();
                    sender.SendCharacter(preview, request.PermId, request.BlockId);
                }
            }

            return (LobbyResults.Ok, null);
        }
        else
        {
            Log.Error($"{sender.ClientName} Requested public data with an invalid block {request}");
            return (LobbyResults.InvalidBlockSelected, null);
        }

    }
    private static (LobbyResults code, string? message) OnReceivePublicData(SacredClient sender, PublicDataMessage data)
    {
        if (data.BlockId == 0)
        {
            //Cheater counter
            return (LobbyResults.ChangePublicDataSuccess, null);
        }
        else if (data.PermId == sender.PermId && data.BlockId == Constants.ProfileBlockId)
        {
            var profile = data.ReadProfileData();

            sender.Profile = profile;

            if (!sender.IsAnonymous)
            {
                Database.SetProfile(sender.PermId, profile);

                int i = 0;
                foreach (var name in profile.CharactersNames)
                {
                    i++;

                    if (string.IsNullOrEmpty(name))
                        continue;

                    var saveFile = Database.GetSaveFile(data.PermId, i);

                    if (saveFile == null)
                        continue;

                    var preview = saveFile.GetCharacterPreview();

                    if (name == preview.Name)
                        continue;

                    preview = preview with
                    {
                        Name = name
                    };

                    saveFile.SetCharacterPreview(preview);

                    Database.SetSaveFile(data.PermId, i, saveFile);
                }
            }

            if (sender.IsInChannel)
            {
                BroadcastProfile(sender.Profile);
            }

            return (LobbyResults.ChangePublicDataSuccess, null);
        }
        // We received a character savegame
        else if (data.BlockId > 0 && data.BlockId <= 8)
        {
            if (!sender.IsServer)
                return (LobbyResults.ErrorNotAGame, "Nice try cheating your save :P");

            using var r = new BinaryReader(new MemoryStream(data.Data));

            var preview = CharacterPreview.Deserialize(r.ReadBytes(556));
            var compLength = r.ReadInt32();
            var uncompLength = r.ReadInt32();
            var compSaveData = r.ReadBytes(compLength);
            var saveData = Utils.ZLibDecompress(compSaveData);
            var saveFile = new SaveFile(saveData);
            Database.SetSaveFile(data.PermId, data.BlockId, saveFile);

            return (LobbyResults.ChangePublicDataSuccess, null);
        }
        else
        {
            Log.Error($"{sender.ClientName} tried to send an invalid block! ({data.BlockId})");
            return (LobbyResults.InvalidBlockSelected, null);
        }
    }
    private static (LobbyResults code, string? message) OnServerLoginRequest(SacredClient sender, ServerInfoMessage serverInfo)
    {
        if (Config.Instance.IsBanned(sender.RemoteEndPoint.Address, BanType.ServerOnly))
        {
            Log.Info($"{sender.ClientName} tried to log in as a server but was refused because it's banned");
            sender.Stop();
            return (LobbyResults.ErrorUserBanned, null);
        }

        IPAddress externalIP = sender.RemoteEndPoint.Address.IsInternal() ? Utils.ExternalIp : sender.RemoteEndPoint.Address;

        sender.ClientType = ClientType.Server;
        sender.ClientName = serverInfo.Name;
        sender.ServerInfo = serverInfo with
        {
            ExternalIp = externalIP,
            ServerId = sender.ConnectionId,
        };

        var channel = ChannelList.FirstOrDefault(x => x.Id == serverInfo.ChannelId);

        if (channel == null)
        {
            Log.Info($"Server {serverInfo.Name} tried to log in into non-existing channel {serverInfo.ChannelId}!");
            sender.Stop();
            return (LobbyResults.GameLoginNotAllowed, "Tried to log in into non-existing channel");
        }

        sender.JoinChannel(serverInfo.ChannelId);

        sender.SendServerLoginResult(externalIP);

        BroadcastServerInfo(sender.ServerInfo);

        Log.Info($"{sender.ClientName} logged in as a server on channel {channel.Name}!");

        return (LobbyResults.Ok, null);
    }

    private static (LobbyResults code, string? message) OnServerChangePublicInfo(SacredClient sender, ServerInfoMessage newInfo)
    {
        var flags = newInfo.Flags;

        // HACK: ClosedNet servers sometimes set the locked flag for no reason when in reality they're fine
        // Work around this...
        if (sender.IsInChannel)
            flags &= ~ServerFlags.Locked;

        sender.ServerInfo = sender.ServerInfo ?? newInfo with
        {
            Flags = flags,
            MaxPlayers = newInfo.MaxPlayers,
            PlayerCount = newInfo.PlayerCount
        };

        BroadcastServerInfo(sender.ServerInfo);
        Log.Info($"GameServer {sender.ClientName} changed public info {sender.ServerInfo.PlayerCount}/{sender.ServerInfo.MaxPlayers} {sender.ServerInfo.Flags}");

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnServerLogout(SacredClient sender, ServerInfoMessage data)
    {
        RemoveServer(sender.ServerInfo!);
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnClientCharacterSelect(SacredClient sender, SelectPublicDataSetMessage data)
    {
        sender.SelectedCharacter = data.BlockId;
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnServerListRequest(SacredClient sender, RequestServerListMessage data)
    {
        var serverList = Servers
            .Where(server => server.ServerInfo != null)
            .Where(server => server.ServerInfo!.ChannelId == sender.Channel)
            .Select(server => 
            {
                // If both server and client are on our machine, patch the IP Address
                if(
                    IPAddress.IsLoopback(sender.RemoteEndPoint.Address) &&
                    IPAddress.IsLoopback(server.RemoteEndPoint.Address))
                {
                    return server.ServerInfo! with
                    {
                        ExternalIp = IPAddress.Loopback
                    };
                }
                else 
                    return server.ServerInfo!;
            })
            .Concat(separators.Select(separator =>
            separator with
            {
                ChannelId = sender.Channel
            }));

        sender.SendServerList(serverList);

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnChannelListRequest(SacredClient sender)
    {
        sender.SendChannelList(ChannelList);

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnChannelJoinRequest(SacredClient sender, JoinChannelMessage data)
    {
        var channel = ChannelList.FirstOrDefault(x => x.Id == data.ChannelId);

        if (channel == null)
        {
            Log.Warning($"{sender.ClientName} tried to join channel with id {data.ChannelId} but it doesn't exist!");
            return (LobbyResults.InternalError, "Tried to join non-existing channel!");
        }

        if (sender.Profile == null)
        {
            Log.Error($"{sender.ClientName} has no profile data. This is a bug!");
            return (LobbyResults.InternalError, "This client doesn't have profile data!");
        }

        sender.JoinChannel(data.ChannelId);
        sender.SendChannelChatMessage();

        foreach (var user in Users)
        {
            if (user.Channel == sender.Channel && user != sender)
            {
                // I know there's a specific message to notify that a user has joined a channel!
                // But just sending the profile data works anyway, saves us a packet and prevents flickering in the user list!

                // Send our data to the other client
                user.SendProfileData(sender.Profile);

                // Send us the client's data
                if (user.Profile != null)
                    sender.SendProfileData(user.Profile);
            }
        }

        foreach (var chatMessage in ChatHistory)
        {
            sender.SendChatMessage(chatMessage with { DestinationPermId = sender.PermId });
        }

        Log.Info($"{sender.ClientName} joined channel {channel.Name}");
        BroadcastSystemMessage($"\\cFFFFFFFF - {sender.ClientName}\\cFF00FF00 joined the channel");

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnChannelLeaveRequest(SacredClient sender)
    {
        if (sender.IsInChannel)
        {
            var channel = ChannelList.First(x => sender.Channel == x.Id);

            foreach (var user in Users)
            {
                if (user.Channel == sender.Channel && user.ConnectionId != sender.ConnectionId)
                    user.OtherUserLeftChannel(sender.PermId, sender.ClientName);
            }

            sender.Channel = -1;

            Log.Info($"{sender.ClientName} left channel {channel.Name}");
            BroadcastSystemMessage($"\\cFFFFFFFF - {sender.ClientName}\\cFFFF0000 left the channel");
        }
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnReceiveChatMessage(SacredClient sender, ChatMessage data)
    {
        data.Deconstruct(out var _, out var _, out var destinationPermId, out var text);

        if (text.StartsWith("/w"))
        {
            var match = Regex.Match(text, @"/w\s+(?<name>.*?)\s+(?<message>.*)");
            if (match.Success)
            {
                var name = match.Groups["name"].ValueSpan;
                var user = GetUserFromPartialName(name);

                if (user != null)
                {
                    text = match.Groups["message"].Value;
                    user.SendChatMessage($"{sender.ClientName} whispers to you", sender.PermId, $"{text}");
                    sender.SendSystemMessage($"You whisper to {user.ClientName}: {text}");
                }
                else
                {
                    sender.SendSystemMessage("Player not found or multiple players found");
                }
            }
            else
            {
                sender.SendSystemMessage("Syntax error! Usage: /w <player> <message>");
            }
        }
        else
        {
            var msg = data with
            {
                SenderName = sender.ClientName,
                SenderPermId = sender.PermId
            };

            BroadcastChatMessage(msg);
            Log.Info($"{sender.ClientName} says: {msg.Text}");
        }

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnAlarm(SacredClient sender)
    {
        Log.Warning($"{sender.ClientName} sent an alarm!");
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnFindUserRequest(SacredClient sender, FindUserMessage data)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnClosedNetNewCharacter(SacredClient sender, ClosedNetNewCharacterMessage data)
    {
        if (sender.IsAnonymous)
        {
            return (LobbyResults.InternalError, "Anonymous accounts cannot play closednet!");
        }

        data.Deconstruct(out var blockId, out var templateId);

        Database.InitSaveFile(sender.PermId, blockId, templateId, sender.Profile!.CharactersNames[data.BlockId - 1]);

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnUserJoinedServer(SacredClient sender, UserJoinedServerMessage data)
    {
        var client = GetClientFromPermId(data.PermId);

        if (client != null)
        {
            var accName = client.ClientName;
            var charName = client.Profile!.SelectedCharacter.Name;
            var gameName = sender.ServerInfo!.Name;
            BroadcastSystemMessage($"\\cFFFFFFFF - {accName}\\cFFFFFFFF joined {gameName}\\cFFFFFFFF with character {charName}");

            Log.Info($"{client.ClientName} joined {sender.ClientName}");
        }

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnUserLeftServer(SacredClient sender, UserLeftServerMessage data)
    {
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnAddFriend(SacredClient sender, BuddyMessage data)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnRemoveFriend(SacredClient sender, BuddyMessage data)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnMessageOfTheDayRequest(SacredClient sender, MotdRequestMessage data)
    {
        var text = Config.Instance.MessageOfTheDay;

        if (!string.IsNullOrWhiteSpace(text))
        {
            sender.SendMessageOfTheDay(data.Id, text);
        }

        return (LobbyResults.Ok, null);
    }
}
