namespace Lobby.Api;

internal record StatisticSample(
    int Servers,
    int Users,
    ulong BytesReceived,
    ulong BytesSent,
    ulong PacketsReceived,
    ulong PacketsSent,
    TimeSpan Runtime,
    TimeSpan AveragePacketProcessingTime
);
