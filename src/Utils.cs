using System.Net;
namespace Sacred;

internal static class Utils
{
    public static Task RunTask(Action action)
    {
        return Task.Factory.StartNew(
            action,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    public static unsafe void FromSpan<T>(ReadOnlySpan<byte> span, out T value) where T : unmanaged
    {
        if (span.Length != sizeof(T))
            throw new ArgumentException($"Mismatched sizes: expected {sizeof(T)} got {span.Length}");

        fixed (byte* p = span)
        {
            value = *(T*)p;
        }
    }

    public static unsafe ReadOnlySpan<byte> ToSpan<T>(in T value) where T : unmanaged
    {
        fixed (T* p = &value)
        {
            return new(p, sizeof(T));
        }
    }

    public static bool IsInternal(IPAddress toTest)
    {
        if (IPAddress.IsLoopback(toTest)) return true;

        byte[] bytes = toTest.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            172 => bytes[1] < 32 && bytes[1] >= 16,
            192 => bytes[1] == 168,
            _ => false,
        };
    }
}
