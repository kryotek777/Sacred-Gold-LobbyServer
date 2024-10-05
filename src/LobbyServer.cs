using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Sacred.Networking;
using Sacred.Networking.Types;
namespace Sacred;

internal static partial class LobbyServer
{
    private static readonly List<Task> tasks = new();
    private static readonly List<ServerInfo> separators = new();
    private static readonly CancellationTokenSource cancellationTokenSource = new();
    private static uint connectionIdCounter = 0;


    private static ConcurrentDictionary<uint, SacredClient> ClientDictionary { get; set; } = new();
    private static IEnumerable<SacredClient> Clients = ClientDictionary.Select(x => x.Value);
    private static IEnumerable<SacredClient> Users => Clients.Where(c => c.ClientType == ClientType.GameClient);
    private static IEnumerable<SacredClient> Servers =  Clients.Where(c => c.ClientType == ClientType.GameServer);
    
    public static Task Start()
    {
        // If the config fails to load for some reason, sane defaults are used
        bool loadedDefaults = Config.Load(out var error);

        // Initialize the log
        Log.Initialize(Config.Instance.LogLevel, Config.Instance.LogPath);

        // Show the previous error
        if(!loadedDefaults)
            Log.Error(error!);

        BuildSeparators();

        tasks.Add(Utils.RunTask(AcceptLoop, cancellationTokenSource.Token));
        tasks.Add(Utils.RunTask(InputLoop, cancellationTokenSource.Token));

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

    public static void SendPacketToAllGameClients(SacredMsgType type, byte[] payload)
    {
        foreach (var client in Users)
        {
            client.SendPacket(type, payload);
        }
    }

    public static void ForEachClient(Action<SacredClient> action)
    {
        foreach (var client in Clients)
        {
            action(client);
        }
    }

    public static SacredClient? GetClientFromPermId(int permId) => Clients.FirstOrDefault(x => permId == (int)x.ConnectionId);

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

    /// <summary>
    /// Notifies other clients that a user has joined a channel and sends them the user list
    /// </summary>
    /// <param name="joining"></param>
    public static void UserJoinedChannel(SacredClient joining)
    {
        lock(joining._lock)
        {
            foreach (var user in Users)
            {
                if(user.IsInChannel && user.ConnectionId != joining.ConnectionId)
                {
                    // I know there's a specific message to notify that a user has joined a channel!
                    // But just sending the profile data works anyway, saves us a packet and prevents flickering in the user list!
                    
                    // Send our data to the other client
                    user.SendProfileData(joining.Profile);
                    
                    // Send us the client's data
                    lock(user._lock)
                        joining.SendProfileData(user.Profile);
                }
            }
        }
    }

    /// <summary>
    /// Notifies other clients that a user has left the channel
    /// </summary>
    /// <param name="permId"></param>
    public static void UserLeftChannel(SacredClient leaving)
    {
        foreach (var user in Users)
        {
            if(user.IsInChannel && user.ConnectionId != leaving.ConnectionId)
                user.OtherUserLeftChannel((int)leaving.ConnectionId);
        }
    }

    private static async void AcceptLoop()
    {
        var listener = new TcpListener(IPAddress.Any, Config.Instance.Port);
        listener.Start();

        Log.Info("Started accepting clients");

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var socket = await listener.AcceptSocketAsync(cancellationTokenSource.Token);
            var remoteIp = (socket.RemoteEndPoint as IPEndPoint)!.Address;

            if (Config.Instance.IsBanned(remoteIp, BanType.Full))
            {
                socket.Close();
                socket.Dispose();
            }
            else
            {
                var connId = ++connectionIdCounter;
                var client = new SacredClient(socket, connId);
                ClientDictionary[connId] = client;
                client.Start();
            }
        }
    }

    private static void BuildSeparators()
    {
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
