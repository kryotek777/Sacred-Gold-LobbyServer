using System.Diagnostics.CodeAnalysis;
using System.Net;
using Lobby.Types.Messages;
using Tomlyn;

namespace Lobby;

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
    public string AllowedUsernameRegex { get; set; }
    public bool StorePersistentData { get; set;}
    public string DatabasePath { get; set; }
    public string SavesPath { get; set; }
    public string TemplatePath { get; set; }
    public bool AllowAnonymousLogin { get; set;}
    public List<ChannelInfo> Channels { get; set; }

    public Config()
    {
        Port = 7066;
        LogPath = "./log.txt";
        LogLevel = LogSeverity.Info;
        MessageOfTheDay = "";
        ChannelChatMessage = "";
        Bans = new();
        ServerSeparators = new();
        ChatHistoryLimit = 0;
        AllowedUsernameRegex = ".*";
        StorePersistentData = false;
        DatabasePath = "";
        AllowAnonymousLogin = true;
        SavesPath = "";
        TemplatePath = "";
        Channels = new();
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

                // Without persistent data we can't restrict logins, the lobby would be useless
                if(Instance.StorePersistentData == false)
                    Instance.AllowAnonymousLogin = true;

                // Fix the channel list to a format that the game likes
                Instance.FixChannelList();

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

    private void FixChannelList()
    {
        // This game is dumb and doesn't actually use the ID specified in the channel struct
        // Instead, it expects the fully ordered list, with empty entries inbetween
        // Just... don't ask. Please.

        var maxId = Channels.Max(x => x.Id);
        var newList = new List<ChannelInfo>(capacity: maxId);

        for (int i = 1; i <= maxId; i++)
        {
            var chan = (Channels.FirstOrDefault(x => x.Id == i) ?? new ChannelInfo()) with
            {
                Id = (ushort)(i - 1)
            };

            newList.Add(chan);
        }

        Channels = newList;
    }
}