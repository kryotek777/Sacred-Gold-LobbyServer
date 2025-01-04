using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lobby.Networking;
using Lobby.Types;
using Lobby.Types.Messages;
using Spectre.Console;

namespace Lobby;

public static class InteractiveConsole
{
    private static readonly Command _exitCommand = new Command(null!, "Exit the interactive console");
    private static readonly Command[] _commands =
    [
        new(List, "List all players and servers"),
        new(Kick, "Kick a client"),
        new(Stop, "Stop and shutdown the lobby"),
        new(Chat, "Send a message"),
        _exitCommand,
    ];

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
            catch(NotSupportedException ex)
            {
                Log.Error($"The interactive console is not supported by your system: {ex.Message}"); 
                break;
            }
            catch(TaskCanceledException)
            {
                throw;
            }
            catch(Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
            finally
            {
                Log.ResumeConsoleOutput();
            }
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
            var name = client.clientName ?? "<unknown>";
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
        prompt.AddChoices(LobbyServer.Clients.Select(x => (x.GetPrintableName(), x)));

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