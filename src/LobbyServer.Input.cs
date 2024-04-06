namespace Sacred;
internal static partial class LobbyServer
{
    private static void InputLoop()
    {
        Log.Info("Started accepting commands. Type 'help' to learn more");

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var line = Console.ReadLine()!;
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            switch (tokens[0])
            {
                case "help":
                    Console.WriteLine(
                        """
                        Common commands:
                        help    |   Prints this message
                        stop    |   Stops the LobbyServer
                        list    |   Lists all the clients
                        """);
                break;

                case "stop":
                    Stop();
                break;

                case "list":
                    List();
                break;
                
                default:
                    Console.WriteLine($"Unknown command '{tokens[0]}'");
                break;
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
    }
}