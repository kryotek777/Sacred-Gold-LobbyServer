using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Lobby.Networking;
using Lobby.Networking.Types;
namespace Lobby;

internal static partial class LobbyServer
{
    private static readonly List<ServerInfo> separators = new();
    private static readonly CancellationTokenSource cancellationTokenSource = new();
    private static uint connectionIdCounter = 0;


    private static ConcurrentDictionary<uint, SacredClient> ClientDictionary { get; set; } = new();
    private static IEnumerable<SacredClient> Clients = ClientDictionary.Select(x => x.Value);
    private static IEnumerable<SacredClient> Users => Clients.Where(c => c.ClientType == ClientType.GameClient);
    private static IEnumerable<SacredClient> Servers = Clients.Where(c => c.ClientType == ClientType.GameServer);

    private static ConcurrentQueue<SacredChatMessage> ChatHistory = new();

    public static Task Run()
    {
        LoadConfig();

        List<Task> tasks =
        [
            AcceptLoopAsync(cancellationTokenSource.Token),
            InputLoopAsync(cancellationTokenSource.Token),
        ];

        return Task.WhenAll(tasks);
    }

    public static void Stop()
    {
        Log.Info("Exiting...");

        cancellationTokenSource.Cancel();
    }

    public static void RemoveClient(SacredClient client)
    {
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
            if (user.IsInChannel && user.clientName!.AsSpan().Contains(name, StringComparison.InvariantCultureIgnoreCase))
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

    public static List<ServerInfo> GetAllServerInfos()
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

    public static void BroadcastServerInfo(ServerInfo info)
    {
        foreach (var user in Users)
        {
            if (user.IsInChannel)
                user.UpdateServerInfo(info);
        }
    }

    public static void RemoveServer(ServerInfo info)
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
    public static void BroadcastChatMessage(SacredChatMessage chatMessage)
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
        lock (joining._lock)
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
                    lock (user._lock)
                        joining.SendProfileData(user.Profile);
                }
            }
        }

        foreach (var chatMessage in ChatHistory)
        {
            joining.SendChatMessage(chatMessage with { DestinationPermId = joining.PermId });
        }

        BroadcastSystemMessage($"\\cFFFFFFFF - {joining.clientName}\\cFF00FF00 joined the channel");
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
                user.OtherUserLeftChannel(leaving.PermId);
        }

        BroadcastSystemMessage($"\\cFFFFFFFF - {leaving.clientName}\\cFFFF0000 left the channel");
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
        separators.AddRange(Config.Instance.ServerSeparators.Select((name, i) => new ServerInfo(
            Name: name,
            LocalIp: IPAddress.None,
            ExternalIp: IPAddress.None,
            Port: 0,
            CurrentPlayers: 0,
            MaxPlayers: 0,
            Flags: 0,
            ServerId: uint.MaxValue - (uint)i, // Give an Id that will never be used in practice
            NetworkProtocolVersion: 0,
            ClientGameVersion: 0,
            ChannelId: 0
        )));
    }
}
