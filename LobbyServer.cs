using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
namespace Sacred;

internal static class LobbyServer
{
    public static readonly ConcurrentBag<Client> clients = new();
    private static uint connectionIdCounter = 0;
    private const int port = 7066;

    public static void Start()
    {
        var t1 = Utils.RunTask(AcceptLoop);
        var t2 = Utils.RunTask(HandleClients);
        Task.WaitAll(t1, t2);
    }

    private static void HandleClients()
    {
        while (true)
        {
            foreach (var client in clients)
            {
                while (client.packets.TryDequeue(out var packet))
                {
                    try
                    {
                        client.HandlePacket(packet);
                    }
                    catch (Exception ex)
                    {
                        var ip = client.socket.RemoteEndPoint as IPEndPoint;
                        Console.WriteLine($"Error handling packet for client {client.connectionId} with ip {ip}: {ex.Message}");
                    }
                }
            }
        }
    }

    private static void AcceptLoop()
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        while (true)
        {
            var socket = listener.AcceptSocket();
            var client = new Client(socket, ++connectionIdCounter);
            clients.Add(client);
            client.Run();
        }
    }
}
