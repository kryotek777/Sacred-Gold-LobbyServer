using Sacred.Networking.Types;

namespace Sacred;
internal static partial class LobbyServer
{
    private static void InputLoop()
    {
        Log.Info("Started accepting commands. Type 'help' to learn more");

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var line = Console.ReadLine()!;
                var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (tokens.Length == 0)
                    continue;

                switch (tokens[0])
                {
                    case "help":
                        Console.WriteLine(
                            """
                        Common commands:
                        help    |   Prints this message
                        stop    |   Stops the LobbyServer
                        list    |   Lists all the clients
                        type    |   Sends a message to clients

                        Debug commands:
                        dbg_join_room #client #room
                        dbg_lobby_result #client #type #result
                        dbg_server_list #client
                        """);
                        break;

                    case "stop":
                        Stop();
                        break;

                    case "list":
                        List();
                        break;

                    case "type":
                        BroadcastMessage(line.Substring(5));
                        break;

                    case "dbg_join_room":
                        {
                            if (
                                int.TryParse(tokens[1], out var client) &&
                                int.TryParse(tokens[2], out var room)
                            )
                            DebugJoinRoom(client, room);
                        }
                    break;

                    case "dbg_lobby_result":
                        {
                            if (
                                int.TryParse(tokens[1], out var client) &&
                                int.TryParse(tokens[2], out var message) &&
                                int.TryParse(tokens[3], out var result)
                            )
                            DebugLobbyResult(client, message, result);
                        }
                    break;

                    case "dbg_server_list":
                        {
                            if (
                                int.TryParse(tokens[1], out var client)
                            )
                            DebugServerList(client);
                        }
                    break;

                    default:
                        Console.WriteLine($"Unknown command '{tokens[0]}'");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex}");
            }
        }

        static void List()
        {
            clientsLock.EnterReadLock();
            Console.WriteLine($"There are {clients.Count} connected clients:");
            foreach (var item in clients.OrderBy(x => x.ClientType))
            {
                Console.WriteLine($"{item.GetPrintableName()} {item.ClientType}");
            }
            clientsLock.ExitReadLock();
        }

        static void BroadcastMessage(string message)
        {
            clientsLock.EnterReadLock();
            foreach (var cl in clients.Where(x => x.ClientType == Networking.ClientType.GameClient))
            {
                cl.SendChatMessage("LobbyServer", message, 0, false);
            }
            clientsLock.ExitReadLock();
        }

        static void DebugJoinRoom(int client, int room)
        {
            clientsLock.EnterReadLock();
            clients.First(x => x.ConnectionId == client).JoinRoom(room);
            clientsLock.ExitReadLock();
        }

        static void DebugLobbyResult(int client, int message, int result)
        {
            clientsLock.EnterReadLock();
            clients.First(x => x.ConnectionId == client).SendLobbyResult((SacredMsgType)message, result);
            clientsLock.ExitReadLock();
        }

        static void DebugServerList(int client)
        {
            clientsLock.EnterReadLock();
            clients.First(x => x.ConnectionId == client).SendServerList();
            clientsLock.ExitReadLock();
        }
    }
}