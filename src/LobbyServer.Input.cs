using Sacred.Networking.Types;

namespace Sacred;
internal static partial class LobbyServer
{
    private static async Task InputLoopAsync(CancellationToken cancToken)
    {
        // Console.In still blocks with async methods
        // This workaround enables us to free the thread that's waiting for input
        // https://learn.microsoft.com/en-us/dotnet/api/system.console.in?view=net-8.0#remarks
        using StreamReader consoleIn = new StreamReader(Console.OpenStandardInput());

        Log.Info("Started accepting commands. Type 'help' to learn more");

        while (!cancToken.IsCancellationRequested)
        {
            try
            {
                var line = await consoleIn.ReadLineAsync(cancToken);

                // EOF received, stop the server
                if(line == null)
                {
                    Stop();
                    break;
                }

                var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (tokens.Length == 0)
                    continue;

                switch (tokens[0])
                {
                    case "help":
                        Console.WriteLine(
                            """
                        Common commands:
                        help                |   Prints this message
                        stop                |   Stops the LobbyServer
                        list                |   Lists all the clients
                        type                |   Sends a message to clients
                        kick <client>       |   Kicks a client
                        reload              |   Reloads the config

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

                    case "kick":
                    {
                        if (int.TryParse(tokens[1], out var client))
                            Kick(client);
                    }
                    break;

                    case "reload":
                        LoadConfig();
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
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex}");
            }
        }

        static void List()
        {
            Console.WriteLine($"There are {ClientDictionary.Count} connected clients:");
            foreach (var item in Clients.OrderBy(x => x.ClientType))
            {
                Console.WriteLine($"{item.GetPrintableName()} {item.ClientType}");
            }
        }

        static void Kick(int client)
        {
            Clients.First(x => x.ConnectionId == client)?.Kick();
        }

        static void BroadcastMessage(string message)
        {
            foreach (var cl in Clients.Where(x => x.ClientType == Networking.ClientType.GameClient))
            {
                cl.SendChatMessage("LobbyServer", 0, message);
            }
        }

        static void DebugJoinRoom(int client, int room)
        {
            Clients.First(x => x.ConnectionId == client).JoinChannel(room);
        }

        static void DebugLobbyResult(int client, int message, int result)
        {
            Clients.First(x => x.ConnectionId == client).SendLobbyResult((LobbyResults)result, (SacredMsgType)message);
        }

        static void DebugServerList(int client)
        {
            Clients.First(x => x.ConnectionId == client).SendServerList();
        }
    }
}