using System.Text.Json;
using System.Text.Json.Serialization;
using Sacred.Networking.Types;

namespace Sacred;

internal record Config(
    int LobbyPort = 7066, 
    LogSeverity LogLevel = LogSeverity.Trace, 
    string LogPath = "log.txt",
    string[]? MOTD = null,
    ServerInfo[]? FakeServers = null,
    Ban[]? BannedClients = null)
{
    private const string configName = "config.json";

    public static Config Instance { get; private set; } = new();

    public static void Load()
    {
        bool exists = File.Exists(configName); 
        if(exists)
        {
            var text = File.ReadAllText(configName);
            Instance = JsonSerializer.Deserialize<Config>(text, new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter() }
            })!;
        }

        if(!exists)
            Log.Warning($"The config file '{configName}' does not exist. Using default values.");

        Log.Info("Config loaded!");
        Log.Trace(Instance.ToString());
    }
}