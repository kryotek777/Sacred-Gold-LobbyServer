using System.Text.Json.Serialization;
using Lobby.DB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lobby.Api;

public static partial class WebApi
{
    private static readonly string ApiVersion = "1.0.0";
    private const string WebPanelFolder = "WebPanel";

    public static async Task Run(string url, CancellationToken token)
    {
        try
        {
            string contentPath = AppContext.BaseDirectory;
            string webRootPath = Path.Combine(AppContext.BaseDirectory, WebPanelFolder);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentPath,
                WebRootPath = webRootPath
            });

            // Prevent default ASP.NET Core logging from clogging the console
            builder.Logging.ClearProviders();

            // Default to enum string serialization
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            if (Config.Instance.EnableSwagger)
            {
                // Add metadata about the minimal API methods
                builder.Services.AddEndpointsApiExplorer();

                // Registers a OpenAPI v3.0 document
                builder.Services.AddOpenApiDocument(settings =>
                {
                    settings.Title = $"LobbyServer API v{ApiVersion}";
                });
            }

            var webApp = builder.Build();

            // Set up the web application to listen on the specified URL
            webApp.Urls.Add(url);

            if (Config.Instance.EnableSwagger)
            {
                // Serves the registered OpenAPI/Swagger documents by default on `/swagger/{documentName}/swagger.json`
                webApp.UseOpenApi();

                // Serves the Swagger UI 3 web ui to view the OpenAPI/Swagger documents by default on `/swagger`
                webApp.UseSwaggerUi(configure =>
                {
                    configure.DocumentTitle = "LobbyServer Swagger";
                });
            }

            if (Config.Instance.EnableWebPanel)
            {
                webApp.UseFileServer();
            }

            // ---[ Generic ]---
            webApp.MapGet("/api/health", Healthcheck).WithTags("Generic");
            webApp.MapGet("/api/version", Version).WithTags("Generic");

            // --- [ Accounts ]---
            webApp.MapGet("/api/accounts/all", AllAccountIds).WithTags("Accounts");
            webApp.MapGet("/api/accounts/by-name/{name}", GetAccountByName).WithTags("Accounts");
            webApp.MapGet("/api/accounts/{id}", GetAccountById).WithTags("Accounts");
            webApp.MapGet("/api/accounts/{id}/characters/{saveId}", GetAccountCharacter).WithTags("Accounts");

            webApp.MapGet("/api/accounts/search/", SearchAccount).WithTags("Accounts");

            // ---[ Statistics ]---
            webApp.MapGet("/api/statistics", AllStatistics).WithTags("Statistics");
            webApp.MapGet("/api/statistics/accounts/count", GetRegisteredUsersCount).WithTags("Accounts", "Statistics");
            webApp.MapGet("/api/statistics/servers", () => LobbyServer.Servers.Count()).WithTags("Statistics");
            webApp.MapGet("/api/statistics/users", () => LobbyServer.Users.Count()).WithTags("Statistics");
            webApp.MapGet("/api/statistics/bytes-received", () => Statistics.BytesReceived).WithTags("Statistics");
            webApp.MapGet("/api/statistics/bytes-sent", () => Statistics.BytesSent).WithTags("Statistics");
            webApp.MapGet("/api/statistics/packets-received", () => Statistics.PacketsReceived).WithTags("Statistics");
            webApp.MapGet("/api/statistics/packets-sent", () => Statistics.PacketsSent).WithTags("Statistics");
            webApp.MapGet("/api/statistics/runtime", () => Statistics.Runtime.TotalSeconds).WithTags("Statistics");
            webApp.MapGet("/api/statistics/average-packet-processing-time", () => Statistics.AveragePacketProcessingTime.TotalMilliseconds).WithTags("Statistics");

            await webApp.StartAsync();

            Log.Info($"WebApi started on {url}");

            if (Config.Instance.EnableWebPanel)
                Log.Info($"WebPanel enabled");

            await webApp.WaitForShutdownAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.Error($"Error in WebApi startup\n{ex}");
        }
        finally
        {
            Log.Info("WebApi stopped");
        }
    }

    #region Main
    /// <summary>
    /// Fallback for all other requests.
    /// This is used to return a 404 Not Found response for any other requests.
    /// </summary>
    private static void Fallback() => TypedResults.NotFound();

    /// <summary>
    /// Healthcheck endpoint.
    /// This is used to check if the server is running and healthy.
    /// It returns "true" if the server is healthy.
    /// </summary>
    private static bool Healthcheck() => true;

    private static Version Version() => new(
        AppVersion: Utils.AppVersion,
        ApiVersion: ApiVersion
    );

    #endregion

    #region Statistics
    /// <summary>
    /// Returns a summary of all statistics.
    /// </summary>
    private static StatisticSample AllStatistics()
    {
        return new StatisticSample(
            LobbyServer.Servers.Count(),
            LobbyServer.Users.Count(),
            Statistics.BytesReceived,
            Statistics.BytesSent,
            Statistics.PacketsReceived,
            Statistics.PacketsSent,
            Statistics.Runtime.TotalSeconds,
            Statistics.AveragePacketProcessingTime.TotalSeconds
        );
    }

    // <summary>
    /// Gets the total number of registered users.
    /// </summary>
    private static int GetRegisteredUsersCount() => Database.GetRegisteredUsers();

    #endregion

    #region Accounts
    /// <summary>
    /// Retrieves all account IDs.
    /// </summary>
    private static List<int> AllAccountIds() => Database.GetAllAccountIds();

    /// <summary>
    /// Retrieves an account by its ID.
    /// Returns 404 if the account is not found.
    /// </summary>
    private static Results<Ok<Account>, NotFound> GetAccountById(int id)
    {
        if (Database.TryGetAccount(id, out var dbAccount))
        {
            return TypedResults.Ok(new Account(dbAccount));
        }
        else
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Retrieves an account by its name.
    /// Returns 404 if the account is not found.
    /// </summary>
    private static Results<Ok<Account>, NotFound> GetAccountByName(string name)
    {
        if (Database.TryGetAccount(name, out var dbAccount))
        {
            return TypedResults.Ok(new Account(dbAccount));
        }
        else
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Retrieves a character for a specific account and save ID.
    /// Returns 404 if the character is not found.
    /// </summary>
    private static Results<Ok<Character>, NotFound> GetAccountCharacter(int id, int saveId)
    {
        if (!Database.TryGetSaveFile(id, saveId, out var saveFile))
            return TypedResults.NotFound();

        var character = Character.FromSaveFile(saveFile);

        return TypedResults.Ok(character);
    }

    /// <summary>
    /// Searches for accounts by name.
    /// Returns all accounts with a name similar to the provided one
    /// </summary>
    private static IEnumerable<Account> SearchAccount(string name)
    {
        foreach (var dbAccount in Database.SearchAccounts(name))
        {
            yield return new Account(dbAccount);
        }
    }
    #endregion
}
