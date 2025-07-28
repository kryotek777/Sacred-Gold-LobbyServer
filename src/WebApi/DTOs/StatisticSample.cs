namespace Lobby.Api;

internal record StatisticSample(
    int Servers,
    int Users,
    ulong BytesReceived,
    ulong BytesSent,
    ulong PacketsReceived,
    ulong PacketsSent,
    double Runtime,
    double AveragePacketProcessingTime
);
