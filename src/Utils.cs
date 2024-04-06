using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Sacred;

internal static class Utils
{
    public static Task RunTask(Action action, CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(
            action,
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    public static Task RunTask(Action action) => RunTask(action, CancellationToken.None);

    public static void FromSpan<T>(ReadOnlySpan<byte> span, out T value) where T : unmanaged
    {
        value = MemoryMarshal.Read<T>(span);
    }

    public static ReadOnlySpan<byte> SliceNullTerminated(this ReadOnlySpan<byte> span)
    {
        return span.Slice(0, span.IndexOf((byte)0));
    }

    public static IPAddress GetExternalIp()
    {
        using var cl = new HttpClient();
        var str = cl.GetStringAsync("http://icanhazip.com").Result;
        var ip = IPAddress.Parse(str.AsSpan().Trim('\n'));
        return ip;
    }

    public static bool IsInternal(this IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        byte[] bytes = ip.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            172 => bytes[1] < 32 && bytes[1] >= 16,
            192 => bytes[1] == 168,
            _ => false,
        };
    }

    public static int ToInt(this IPAddress ip)
    {
        return BitConverter.ToInt32(ip.GetAddressBytes());
    }
}
