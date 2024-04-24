using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
namespace Sacred;

internal static class Utils
{
    public static readonly Encoding Windows1252Encoding;

    static Utils()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Windows1252Encoding = Encoding.GetEncoding(1252);
    }

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

    public static void FormatBytes(ReadOnlySpan<byte> data, StringBuilder sb)
    {
        const int lineLength = 8;
        for (int i = 0; i < data.Length;)
        {
            var b = data[i];
            char c = (char)b;

            sb.Append($"{b:X2} [{(char.IsControl(c) ? ' ' : c)}] ");

            if (++i % lineLength == 0)
                sb.AppendLine();
        }
    }

    public static string FormatBytes(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder();
        FormatBytes(data, sb);
        return sb.ToString();
    }

    public static string Win1252ToString(ReadOnlySpan<byte> span) => Windows1252Encoding.GetString(span.SliceNullTerminated());
    public static void StringToWin1252(string str, Span<byte> span)
    {
        int bytesWritten = Windows1252Encoding.GetBytes(str, span);
        span.Slice(bytesWritten).Clear();
    }
}
