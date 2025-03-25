using System.Collections.Concurrent;
using System.Diagnostics;

namespace Lobby;

public static class Statistics
{
    public static int Servers => LobbyServer.Servers.Count();
    public static int Users => LobbyServer.Users.Count();
    public static ulong BytesReceived => _bytesReceived;
    public static ulong BytesSent => _bytesSent;
    public static ulong PacketsReceived => _packetsReceived;
    public static ulong PacketsSent => _packetsSent;
    public static TimeSpan Runtime => _stopwatch.Elapsed;
    public static TimeSpan AveragePacketProcessingTime => _averagePacketProcessingTime;

    private const int maxPacketTimeSamples = 100;
    private static bool _enabled = Config.Instance.CollectStatistics;
    private static ulong _bytesReceived = 0;
    private static ulong _bytesSent = 0;
    private static ulong _packetsReceived = 0;
    private static ulong _packetsSent = 0;
    private static Stopwatch _stopwatch = new();
    private static ConcurrentQueue<TimeSpan> _processingTimes = new();
    private static TimeSpan _packetProcessingStart = TimeSpan.Zero;
    private static TimeSpan _averagePacketProcessingTime = TimeSpan.Zero;
    
    public static void Initialize()
    {
        if(_enabled)
        {
            _stopwatch.Start();
        }
    }

    public static void AddBytesReceived(ulong bytes)
    {
        if (_enabled)
        {
            Interlocked.Add(ref _bytesReceived, bytes);
        }
    }

    public static void AddBytesSent(ulong bytes)
    {
        if (_enabled)
        {
            Interlocked.Add(ref _bytesSent, bytes);
        }
    }

    public static void IncrementPacketsReceived()
    {
        if (_enabled)
        {
            Interlocked.Increment(ref _packetsReceived);
        }
    }

    public static void IncrementPacketsSent()
    {
        if (_enabled)
        {
            Interlocked.Increment(ref _packetsSent);
        }
    }

    public static void StartProcessingPacket()
    {
        if (_enabled)
        {
            _packetProcessingStart = _stopwatch.Elapsed;
        }
    }

    public static void EndProcessingPacket()
    {
        if (_enabled)
        {
            var processingTime = _stopwatch.Elapsed - _packetProcessingStart;
            _processingTimes.Enqueue(processingTime);

            if (_processingTimes.Count > maxPacketTimeSamples)
            {
                _processingTimes.TryDequeue(out _);
            }

            var sum = TimeSpan.Zero;
            foreach (var sample in _processingTimes)
            {
                sum += sample;
            }

            _averagePacketProcessingTime = sum / _processingTimes.Count;

        }
    }
}