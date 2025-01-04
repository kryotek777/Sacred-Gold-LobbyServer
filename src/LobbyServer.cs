using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Channels;
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
    private static ConcurrentQueue<ChatMessage> ChatHistory = new();

    public static Task Run()
    {
        LoadConfig();

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
    }

    public static void ReceivePacket(SacredPacket packet)
    {
        // No need to check the returned bool, this will always succeed with an unbounded channel
        ReceivedPackets.Writer.TryWrite(packet);
    }

    public static void RemoveClient(SacredClient client)
    {
        if (client.IsUser)
            OnChannelLeaveRequest(client);

        if (client.IsServer)
            OnServerLogout(client, client.ServerInfo!);

        ClientDictionary.Remove(client.ConnectionId, out _);
        Log.Info($"Client removed {client.GetPrintableName()}");
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

    public static List<ServerInfoMessage> GetAllServerInfos()
    {
        var serverList = Servers
            .Where(x => x.ServerInfo != null)
            .Select(x => x.ServerInfo)
            .Concat(separators)
            .Where(x => x != null)
            .ToList();

        return serverList!;
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

    /// <summary>
    /// Notifies other clients that a user has joined a channel and sends them the user list
    /// </summary>
    /// <param name="joining">The user that's joining the channel</param>
    public static void UserJoinedChannel(SacredClient joining)
    {

        foreach (var user in Users)
        {
            if (user.IsInChannel && user.ConnectionId != joining.ConnectionId)
            {
                // I know there's a specific message to notify that a user has joined a channel!
                // But just sending the profile data works anyway, saves us a packet and prevents flickering in the user list!

                // Send our data to the other client
                user.SendProfileData(joining.Profile);

                // Send us the client's data
                joining.SendProfileData(user.Profile);
            }
        }

        foreach (var chatMessage in ChatHistory)
        {
            joining.SendChatMessage(chatMessage with { DestinationPermId = joining.PermId });
        }

        BroadcastSystemMessage($"\\cFFFFFFFF - {joining.ClientName}\\cFF00FF00 joined the channel");
    }

    /// <summary>
    /// Notifies other clients that a user has left the channel
    /// </summary>
    /// <param name="leaving">The user that's leaving the channel</param>
    public static void UserLeftChannel(SacredClient leaving)
    {
        foreach (var user in Users)
        {
            if (user.IsInChannel && user.ConnectionId != leaving.ConnectionId)
                user.OtherUserLeftChannel(leaving.PermId, leaving.ClientName);
        }

        BroadcastSystemMessage($"\\cFFFFFFFF - {leaving.ClientName}\\cFFFF0000 left the channel");
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
        try
        {
            var reader = ReceivedPackets.Reader;

            while (!token.IsCancellationRequested)
            {
                var packet = await reader.ReadAsync(token);
                packet.Deconstruct(out var sender, out var type, out var payload);
                //sender.ReceivePacket(type, payload);
                ProcessPacket(packet);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
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
            case SacredMsgType.PublicDataRequest:
                {
                    var data = PublicDataRequestMessage.Deserialize(payload);
                    result = OnPublicDataRequest(sender, data);
                    break;
                }
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
            case SacredMsgType.ReceiveClientPublicDataFromServer:
                {
                    var data = PublicDataMessage.Deserialize(payload);
                    result = OnReceiveClientPublicDataFromServer(sender, data);
                    break;
                }
            case SacredMsgType.ServerRequestsClientsPublicData:
                {
                    var data = PublicDataRequestMessage.Deserialize(payload);
                    result = OnServerRequestsClientsPublicData(sender, data);
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

        var msg = new ResultMessage(result.code, type);
        sender.SendPacket(SacredMsgType.LobbyResult, msg);

        if (!string.IsNullOrWhiteSpace(result.message))
        {
            sender.SendImportantMessage(result.message);
        }
    }

    private static (LobbyResults code, string? message) OnClientRegistrationRequest(SacredClient sender, RegistrationMessage data)
    {
        return (LobbyResults.InternalError, "Registration is not supported. You can log in with any username and password you wish");
    }
    private static (LobbyResults code, string? message) OnClientLoginRequest(SacredClient sender, LoginMessage loginRequest)
    {
        if (Config.Instance.IsBanned(sender.RemoteEndPoint.Address, BanType.ClientOnly))
        {
            Log.Info($"{sender.GetPrintableName()} tried to log in as a user but was refused because it's banned");
            sender.Stop();
            return (LobbyResults.ErrorUserBanned, null);
        }

        sender.ClientName = loginRequest.Username;
        if (Regex.IsMatch(sender.ClientName, Config.Instance.AllowedUsernameRegex))
        {
            sender.ClientType = ClientType.User;

            var loginResult = new LoginResultMessage(
                Result: LobbyResults.Ok,
                Ip: sender.RemoteEndPoint.Address,
                PermId: sender.PermId,
                Message: "Welcome!"
            );

            sender.SendPacket(SacredMsgType.ClientLoginResult, loginResult);
            Log.Info($"{sender.GetPrintableName()} logged in as a user!");

            return (LobbyResults.Ok, null);
        }
        else
        {
            Log.Info($"{sender.GetPrintableName()} tried to login with an invalid username {sender.ClientName}");

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
        if (request.BlockId == Constants.ProfileBlockId)
        {
            var client = GetClientFromPermId(request.PermId);

            if (client != null)
            {
                var data = client.Profile;
                sender.SendProfileData(data);
                return (LobbyResults.Ok, null);
            }
            else
            {
                Log.Warning($"{sender.GetPrintableName()} Requested public data of an invalid player: {request.PermId}");
                return (LobbyResults.ErrorUserNotFound, null);
            }
        }
        else if (request.BlockId <= 8)
        {
            Log.Error($"{sender.GetPrintableName()} Character requests aren't implemented yet!");
            return (LobbyResults.InternalError, "Closed net isn't implemented yet!");
        }
        else
        {
            Log.Error($"{sender.GetPrintableName()} Requested public data with an invalid block {request}");
            return (LobbyResults.InvalidBlockSelected, null);
        }
    }
    private static (LobbyResults code, string? message) OnReceivePublicData(SacredClient sender, PublicDataMessage data)
    {
        if (data.PermId == sender.PermId && data.BlockId == Constants.ProfileBlockId)
        {
            sender.Profile = data.ReadProfileData();

            sender.SendLobbyResult(LobbyResults.ChangePublicDataSuccess, SacredMsgType.ReceivePublicData);

            if (sender.IsInChannel)
            {

                BroadcastProfile(sender.Profile);
            }
            return (LobbyResults.Ok, null);
        }

        return (LobbyResults.InvalidBlockSelected, null);
    }
    private static (LobbyResults code, string? message) OnServerLoginRequest(SacredClient sender, ServerInfoMessage serverInfo)
    {
        if (Config.Instance.IsBanned(sender.RemoteEndPoint.Address, BanType.ServerOnly))
        {
            Log.Info($"{sender.GetPrintableName()} tried to log in as a server but was refused because it's banned");
            sender.Stop();
            return (LobbyResults.ErrorUserBanned, null);
        }

        sender.ClientType = ClientType.Server;
        IPAddress externalIP = sender.RemoteEndPoint.Address.IsInternal() ? Utils.GetExternalIp() : sender.RemoteEndPoint.Address;

        sender.SendPacket(SacredMsgType.ServerLoginResult, externalIP.GetAddressBytes());


        sender.ServerInfo = serverInfo with
        {
            ExternalIp = externalIP,
            ServerId = sender.ConnectionId,
            ChannelId = 0
        };

        BroadcastServerInfo(sender.ServerInfo);
        Log.Info($"{sender.GetPrintableName()} logged in as a server!");


        return (LobbyResults.Ok, null);
    }

    private static (LobbyResults code, string? message) OnServerChangePublicInfo(SacredClient sender, ServerInfoMessage newInfo)
    {
        sender.ServerInfo = sender.ServerInfo! with
        {
            Flags = newInfo.Flags,
            MaxPlayers = newInfo.MaxPlayers,
            PlayerCount = newInfo.PlayerCount
        };

        BroadcastServerInfo(sender.ServerInfo);
        Log.Info($"GameServer {sender.GetPrintableName()} changed public info {sender.ServerInfo.PlayerCount}/{sender.ServerInfo.MaxPlayers} {sender.ServerInfo.Flags}");

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnServerLogout(SacredClient sender, ServerInfoMessage data)
    {
        RemoveServer(sender.ServerInfo!);
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnClientCharacterSelect(SacredClient sender, SelectPublicDataSetMessage data)
    {
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnReceiveClientPublicDataFromServer(SacredClient sender, PublicDataMessage data)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnServerRequestsClientsPublicData(SacredClient sender, PublicDataRequestMessage data)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnServerListRequest(SacredClient sender, RequestServerListMessage data)
    {
        var id = data.ChannelId;

        Log.Warning($"{sender.GetPrintableName()} requested the server list for channel {id}, but channels aren't implemented yet!");

        sender.SendServerList();

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnChannelListRequest(SacredClient sender)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnChannelJoinRequest(SacredClient sender, JoinChannelMessage data)
    {
        int channel = data.ChannelId;
        Log.Warning($"{sender.GetPrintableName()} asked to join channel {channel}, but channels aren't implemented yet!");

        sender.JoinChannel(0);
        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnChannelLeaveRequest(SacredClient sender)
    {
        if (sender.Channel != -1)
        {
            sender.Channel = -1;
            UserLeftChannel(sender);
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
            Log.Info($"{sender.GetPrintableName()} says: {msg.Text}");
        }

        return (LobbyResults.Ok, null);
    }
    private static (LobbyResults code, string? message) OnAlarm(SacredClient sender)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnFindUserRequest(SacredClient sender, FindUserMessage data)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnClosedNetNewCharacter(SacredClient sender, ClosedNetNewCharacterMessage data)
    {
        return (LobbyResults.InternalError, "Not implemented yet");
    }
    private static (LobbyResults code, string? message) OnUserJoinedServer(SacredClient sender, UserJoinedServerMessage data)
    {
        var client = GetClientFromPermId(data.PermId);

        if (client != null)
        {
            var accName = client.ClientName;
            var charName = client.Profile.SelectedCharacter.Name;
            var gameName = sender.ServerInfo!.Name;
            BroadcastSystemMessage($"\\cFFFFFFFF - {accName}\\cFFFFFFFF joined {gameName}\\cFFFFFFFF with character {charName}");

            Log.Info($"{client.GetPrintableName()} joined {sender.GetPrintableName()}");
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
        var id = data.Id;
        var text = Config.Instance.MessageOfTheDay;

        if (!string.IsNullOrWhiteSpace(text))
        {
            var msg = new MotdMessage(id, text);
            sender.SendPacket(SacredMsgType.SendMessageOfTheDay, msg);
        }

        return (LobbyResults.Ok, null);
    }
}
