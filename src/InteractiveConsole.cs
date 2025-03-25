using System.Net;
using System.Numerics;
using Lobby.Networking;
using Spectre.Console;

namespace Lobby;

public static class InteractiveConsole
{
    private static readonly Command _exitCommand = new Command(null!, "Exit the interactive console");
    private static List<Command> _commands = new();

    public static void Initialize()
    {
        if(Config.Instance.CollectStatistics)
            _commands.Add(new(Statistics, "Show statistics"));

        _commands.Add(new(List, "List all players and servers"));
        _commands.Add(new(Kick, "Kick a client"));
        _commands.Add(new(Stop, "Stop and shutdown the lobby"));
        _commands.Add(new(Chat, "Send a message"));
        _commands.Add(_exitCommand);
    }

    public static void Run(CancellationToken cancToken)
    {
        while (!cancToken.IsCancellationRequested)
        {
            try
            {
                Log.Info("Press ENTER to start the interactive console");

                WaitForEnter();

                AnsiConsole.AlternateScreen(() =>
                {
                    Log.PauseConsoleOutput();

                    var commandsPrompt = new SelectionPrompt<Command>().AddChoices(_commands);

                    while (!cancToken.IsCancellationRequested)
                    {
                        var command = AnsiConsole.Prompt(commandsPrompt);

                        if (command == _exitCommand)
                        {
                            break;
                        }
                        else
                        {
                            command.Action();
                        }
                    }
                });
            }
            catch (NotSupportedException ex)
            {
                Log.Error($"The interactive console is not supported by your system: {ex.Message}");
                break;
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
            finally
            {
                Log.ResumeConsoleOutput();
            }
        }
    }

    private static void Statistics()
    {
        var table = new Table();

        AnsiConsole.Live(table)
        .AutoClear(true)
        .Start(ctx =>
        {
            while (!Console.KeyAvailable)
            {
                table.AddColumn("Name");
                table.AddColumn("Value");

                table.AddRow("Servers", Lobby.Statistics.Servers.ToString());
                table.AddRow("Users", Lobby.Statistics.Users.ToString());
                table.AddRow("Avg. Packet Wait Time", $"{Lobby.Statistics.AveragePacketWaitTime.TotalMilliseconds:F2}ms");
                table.AddRow("Avg. Packet Processing Time", $"{Lobby.Statistics.AveragePacketProcessingTime.TotalMilliseconds:F2}ms");
                table.AddRow("Bytes received", FormatBytes(Lobby.Statistics.BytesReceived));
                table.AddRow("Bytes sent", FormatBytes(Lobby.Statistics.BytesSent));
                table.AddRow("Packets received", Lobby.Statistics.PacketsReceived.ToString());
                table.AddRow("Packets sent", Lobby.Statistics.PacketsSent.ToString());
                table.AddRow("Runtime", Lobby.Statistics.Runtime.ToString(@"hh\:mm\:ss"));

                ctx.Refresh();

                Thread.Sleep(500);

                table = new Table();

                ctx.UpdateTarget(table);
            }
        });

        static string FormatBytes(ulong count)
        {
            string[] suffixes = { "B", "KiB", "MiB", "GiB" };

            var index = BitOperations.Log2(count) / 10;
            var divisor = 1u << (10 * index);

            return $"{(double)count / divisor:F2} {suffixes[index]} ({count})";
        }
    }

    private static void List()
    {
        var table = new Table();

        table.AddColumn("Name");
        table.AddColumn("Type");
        table.AddColumn("Address");

        foreach (var client in LobbyServer.Clients)
        {
            var name = client.ClientName;
            var type = client.ClientType.ToString();
            var address = client.RemoteEndPoint.ToString();
            table.AddRow(name, type, address);
        }

        AnsiConsole.Write(table);
        Pause();
    }

    private static void Kick()
    {
        var client = PromptClient();
        client?.Kick();
    }

    private static void Stop()
    {
        LobbyServer.Stop();
    }

    private static void Chat()
    {
        var msg = AnsiConsole.Ask<string>("Send a message: ");
        LobbyServer.BroadcastSystemMessage($"Admin: {msg}");
    }

    private static SacredClient? PromptClient()
    {
        var prompt = new SelectionPrompt<(string, SacredClient)>()
            .Title("Who do you want to kick?")
            .UseConverter(x => x.Item1);

        prompt.AddChoice(("No one", null!));
        prompt.AddChoices(LobbyServer.Clients.Select(x => (x.ClientName, x)));

        return AnsiConsole.Prompt(prompt).Item2;
    }

    private static void Pause()
    {
        AnsiConsole.WriteLine("Press ENTER to continue...");
        WaitForEnter();
    }

    private static void WaitForEnter()
    {
        Console.ReadLine();
    }
}

record Command(Action Action, string Description)
{
    public override string ToString() => Description;
}

record Client(string Name, IPEndPoint EndPoint, ClientType Type);