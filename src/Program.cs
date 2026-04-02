using Lobby;

Console.CancelKeyPress += (s, e) => LobbyServer.Stop();

await LobbyServer.Run();

Log.Info("Exited");