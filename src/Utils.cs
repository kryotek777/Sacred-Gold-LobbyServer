using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
namespace Lobby;

internal static class Utils
{
    public static readonly Encoding Windows1252Encoding;
    public static readonly IPAddress ExternalIp; 
    private const string EncryptionKey = "Sacred";

    static Utils()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Windows1252Encoding = Encoding.GetEncoding(1252);

        ExternalIp = GetExternalIp();
    }

    public static Task RunTask(Action<CancellationToken> action, CancellationToken token)
    {
        return Task.Run(
            () => action(token),
            token
        );
    }

    public static unsafe void FromSpan<T>(ReadOnlySpan<byte> span, out T value) where T : unmanaged
    {
        Debug.Assert(span.Length <= Unsafe.SizeOf<T>());

        value = new();

        fixed (byte* spanPtr = &span[0])
        fixed (T* valuePtr = &value)
        {
            Unsafe.CopyBlockUnaligned(valuePtr, spanPtr, (uint)span.Length);
        }
    }

    public static byte[] ToArray<T>(in T value) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        var array = new byte[size];
        MemoryMarshal.Write(array, value);
        return array;
    }

    public static ReadOnlySpan<byte> SliceNullTerminated(this ReadOnlySpan<byte> span)
    {
        var index = span.IndexOf((byte)0);
        //Keep the original span if no null terminator exists
        var result = index == -1 ? span : span.Slice(0, index);

        return result;
    }

    private static IPAddress GetExternalIp()
    {
        using var cl = new HttpClient();
        var str = cl.GetStringAsync("http://ipv4.icanhazip.com").Result;
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

    public static string DeserializeString(ReadOnlySpan<byte> data) => Win1252ToString(data);
    public static void SerializeString(string str, Span<byte> data) => StringToWin1252(str, data);

    public static string Win1252ToString(ReadOnlySpan<byte> span) => Windows1252Encoding.GetString(span.SliceNullTerminated());
    public static void StringToWin1252(string str, Span<byte> span)
    {
        int bytesWritten = Windows1252Encoding.GetBytes(str, span);
        span.Slice(bytesWritten).Clear();
    }

    public static byte[] StringToWin1252(string str)
    {
        return Windows1252Encoding.GetBytes(str);
    }

    public static byte[] StringToWin1252(string str, int length)
    {
        var buf = new byte[length];

        if (Windows1252Encoding.GetByteCount(str) > length)
        {
            Log.Error($"The string '{str}' doesn't fit in {length} bytes and will be cut off, this is a bug!");
        }

        Windows1252Encoding.GetBytes(str, buf);
        return buf;
    }

    public static byte[] StringToUtf16(string str, int length)
    {
        var buf = new byte[length];
        Encoding.Unicode.GetBytes(str, buf);
        return buf;
    }

    public static string Utf16ToString(ReadOnlySpan<byte> data)
    {
        return Encoding.Unicode.GetString(data).Trim('\0');
    }

    public static string TincatDecrypt(ReadOnlySpan<byte> data)
    {
        int num = 63;
        Span<byte> result = stackalloc byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            num = unchecked((data[i] ^ EncryptionKey[i % EncryptionKey.Length]) - i - num);
            result[i] = (byte)num;
        }

        return DeserializeString(result);
    }

    public static void TincatEncrypt(string text, Span<byte> data)
    {
        int num = 63;

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)unchecked((text[i] + i + num) ^ EncryptionKey[i % EncryptionKey.Length]);
            num = text[i];
        }
    }

    public static byte[] ZLibCompress(byte[] data)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var zlibStream = new ZLibStream(memoryStream, CompressionLevel.Optimal))
            {
                zlibStream.Write(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
    }

    // Function to decompress data using ZlibStream
    public static byte[] ZLibDecompress(byte[] data)
    {
        using (var memoryStream = new MemoryStream(data))
        {
            using (var zlibStream = new ZLibStream(memoryStream, CompressionMode.Decompress))
            {
                using (var outputStream = new MemoryStream())
                {
                    zlibStream.CopyTo(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
    }
}
