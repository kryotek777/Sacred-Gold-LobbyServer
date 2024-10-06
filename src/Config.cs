using System.Diagnostics.CodeAnalysis;
using System.Net;
using Tomlyn;

namespace Sacred;

public class Config
{
    private const string configPath = "config.toml";

    public static Config Instance { get; private set; } = null!;
    public int Port { get; set; }
    public string LogPath { get; set; }
    public LogSeverity LogLevel { get; set; }
    public string MessageOfTheDay { get; set; }
    public string ChannelChatMessage { get; set; }
    public List<Ban> Bans { get; set; }
    public List<string> ServerSeparators { get; set; }
    public uint ChatHistoryLimit { get; set; }

    public Config()
    {
        Port = 7066;
        LogPath = "./log.txt";
        LogLevel = LogSeverity.Info;
        MessageOfTheDay = "";
        ChannelChatMessage = "";
        Bans = new();
        ServerSeparators = new();
    }

    public static bool Load([NotNullWhen(false)] out string? error)
    {
        try
        {
            if (File.Exists(configPath))
            {
                var text = File.ReadAllText(configPath);

                var options = new TomlModelOptions()
                {
                    //Don't convert names to snake_case (Yeah. It does by default.)
                    ConvertPropertyName = (x) => x,

                    //Custom parser for IpAddress
                    ConvertToModel = (src, type) =>
                    {
                        if (type == typeof(IPAddress) && src is string str)
                        {
                            return IPAddress.Parse(str);
                        }
                        else
                        {
                            return null;
                        }
                    }
                };

                Instance = Toml.ToModel<Config>(text, options: options);
                error = null;
                return true;
            }
            else
            {
                Instance = new();
                error = "Config file doesn't exist! Loading default values...";
                return false;
            }
        }
        catch (Exception ex)
        {
            Instance = new();
            error = $"Error loading config, using default values: {ex}";
            return false;
        }
    }

    public bool IsBanned(IPAddress ip, BanType banType) =>
        banType != BanType.None && Bans.Any(x => x.Ip.Equals(ip) && x.BanType == banType);
}