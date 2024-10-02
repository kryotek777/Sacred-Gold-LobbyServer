using System.Net;

namespace Sacred;

public class Ban
{
    public IPAddress Ip { get; init; }
    public BanType BanType { get; init; }
    public string Reason { get; init; }

    public Ban()
    {
        Ip = IPAddress.None;
        Reason = "";
    }
}
