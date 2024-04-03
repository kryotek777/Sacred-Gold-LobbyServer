using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sacred;

internal record Config(int LobbyPort = 7066, LogSeverity LogLevel = LogSeverity.Trace)
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

        Log.LogLevel = Instance.LogLevel;

        if(!exists)
            Log.Warning($"The config file '{configName}' does not exist. Using default values.");

        Log.Info("Config loaded!");
        Log.Trace(Instance.ToString());
    }
}