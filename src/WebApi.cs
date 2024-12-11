using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sacred;

public static class WebApi
{
    public static async Task Run(string url, CancellationToken token)
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.Logging.ClearProviders();

        var webApp = builder.Build();

        webApp.MapGet("/health", Healthcheck);
        webApp.MapGet("/stats/opennet/users", UserList);
        webApp.MapGet("/stats/opennet/servers", ServerList);
        webApp.MapFallback(Fallback);

        webApp.Urls.Add(url);

        await webApp.StartAsync();

        Log.Info($"WebApi started on {url}");

        token.WaitHandle.WaitOne();

        await webApp.StopAsync();

        Log.Info("WebApi stopped");
    }

    private static void Fallback() => TypedResults.NotFound();

    private static bool Healthcheck() => true;

    private static IEnumerable<User> UserList()
    {
        return LobbyServer.Users
            .Where(x => x.IsInChannel)
            .Select(x => new User(x.clientName!, x.Profile.SelectedCharacter.Name));
    }

    private static IEnumerable<Server> ServerList()
    {
        return LobbyServer.Servers
            .Select(x => new Server(x.ServerInfo!.Name, x.ServerInfo.CurrentPlayers, x.ServerInfo.MaxPlayers)); 
    }

    private record User(string username, string character);
    private record Server(string name, int players, int maxPlayers);
}