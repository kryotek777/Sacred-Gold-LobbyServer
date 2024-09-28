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
    private static readonly CancellationTokenSource cancellationTokenSource = new();
    private static uint connectionIdCounter = 0;

    public static BanList? BanList { get; private set; }
    
    public static Task Start()
    {
        Config.Load();

        Log.Initialize(Config.Instance.LogLevel, Config.Instance.LogPath);

        var bannedClients = Config.Instance.BannedClients;
        if(bannedClients != null)
            BanList = new(bannedClients);

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

    public static void SendPacketToAllGameClients(TincatPacket packet)
    {
        clientsLock.EnterReadLock();
        foreach (var client in clients.Where(x => x.ClientType == ClientType.GameClient))
        {
            client.SendPacket(packet);
        }
        clientsLock.ExitReadLock();
    }

    public static void SendPacketToAllGameServers(TincatPacket packet)
    {
        clientsLock.EnterReadLock();
        foreach (var client in clients.Where(x => x.ClientType == ClientType.GameServer))
        {
            client.SendPacket(packet);
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

        var fakeServers = Config.Instance.FakeServers ?? Enumerable.Empty<ServerInfo>();

        var serverList = clients
            .Where(x => x.ClientType == ClientType.GameServer && x.ServerInfo != null)
            .Select(x => x.ServerInfo)
            .Concat(fakeServers)
            .Where(x => x != null)
            .ToList();

        clientsLock.ExitReadLock();

        return serverList!;
    }

    private static async void AcceptLoop()
    {
        var listener = new TcpListener(IPAddress.Any, Config.Instance.LobbyPort);
        listener.Start();

        Log.Info("Started accepting clients");

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var socket = await listener.AcceptSocketAsync(cancellationTokenSource.Token);
            var remoteIp = (socket.RemoteEndPoint as IPEndPoint)!.Address;

            if (BanList?.IsBanned(remoteIp, BanType.Full) == true)
            {
                socket.Close();
                socket.Dispose();
            }
            else
            {
                var connection = new SacredConnection(socket);
                var client = new SacredClient(connection, ++connectionIdCounter);
                client.Start();
            }
        }
    }
}
