using System.Net;
using System.Text.Json.Serialization;

namespace Sacred;

public enum BanType
{
    //Everything is allowed
    None,

    //Fully ban the IP address from even attempting to connect
    Full,

    //Forbid only game clients
    ClientOnly,

    //Forbid only game servers
    ServerOnly,
}

public record Ban(
    [property: JsonConverter(typeof(IPAddressConverter))]
    IPAddress Ip, 
    BanType Type);

public class BanList
{
    private List<Ban> banList;

    [JsonConstructor]
    public BanList(IEnumerable<Ban> bans)
    {
        banList = bans.ToList();
    }

    public bool IsBanned(IPAddress ip, BanType banType) =>
        banType != BanType.None && banList.Any(x => x.Ip.Equals(ip) && x.Type == banType);
}