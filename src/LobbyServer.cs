using System.Net;
using System.Net.Sockets;
using Sacred.Networking;
using Sacred.Networking.Types;
namespace Sacred;

internal static partial class LobbyServer
{
    private static readonly ReaderWriterLockSlim clientsLock = new(LockRecursionPolicy.SupportsRecursion);
    private static readonly List<SacredClient> clients = new();
    private static readonly List<Task> tasks = new();
    private static readonly List<ServerInfo> separators = new();
    private static readonly CancellationTokenSource cancellationTokenSource = new();
    private static uint connectionIdCounter = 0;

    private static IEnumerable<SacredClient> Users => clients.Where(c => c.ClientType == ClientType.GameClient);
    
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

    public static void AddClient(SacredClient client)
    {
        clientsLock.EnterWriteLock();
        clients.Add(client);
        clientsLock.ExitWriteLock();
    }

    public static void RemoveClient(SacredClient client)
    {
        clientsLock.EnterWriteLock();
        clients.Remove(client);
        clientsLock.ExitWriteLock();
        Log.Info($"Client removed {client.GetPrintableName()}");
    }

    public static void SendPacketToAllGameClients(SacredMsgType type, byte[] payload)
    {
        clientsLock.EnterReadLock();
        foreach (var client in clients.Where(x => x.ClientType == ClientType.GameClient))
        {
            client.SendPacket(type, payload);
        }
        clientsLock.ExitReadLock();
    }

    public static void ForEachClient(Action<SacredClient> action)
    {
        clientsLock.EnterReadLock();
        foreach (var client in clients)
        {
            action(client);
        }
        clientsLock.ExitReadLock();
    }

    public static SacredClient? GetClientFromPermId(int permId)
    {
        clientsLock.EnterReadLock();
        var cl = clients.FirstOrDefault(x => permId == (int)x.ConnectionId);
        clientsLock.ExitReadLock();
        return cl;
    }

    public static List<ServerInfo> GetAllServerInfos()
    {
        clientsLock.EnterReadLock();

        var serverList = clients
            .Where(x => x.ClientType == ClientType.GameServer && x.ServerInfo != null)
            .Select(x => x.ServerInfo)
            .Concat(separators)
            .Where(x => x != null)
            .ToList();

        clientsLock.ExitReadLock();

        return serverList!;
    }

    public static void UserLeftChannel(int permId)
    {
        clientsLock.EnterReadLock();
        foreach (var user in Users)
        {
            if(user.IsInChannel && (int)user.ConnectionId != permId)
                user.OtherUserLeftChannel(permId);
        }
        clientsLock.ExitReadLock();
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
                var client = new SacredClient(socket, ++connectionIdCounter);
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
