using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Sacred.Types;
namespace Sacred;

class Client
{
    public readonly Socket socket;
    public readonly NetworkStream stream;
    public readonly ConcurrentQueue<TincatPacket> packets;
    public Task? readLoopTask;
    public uint connectionId;
    public ServerInfo serverInfo;
    public bool isServer;

    public Client(Socket socket, uint connectionId)
    {
        this.socket = socket;
        this.stream = new(socket);
        this.packets = new();
        this.connectionId = connectionId;
        this.serverInfo = default;
        this.isServer = false;
    }

    public Task Run()
    {
        return readLoopTask = Utils.RunTask(ReadLoop);
    }

    private void ReadLoop()
    {
        TincatHeader header = default;

        Log.Info($"Incoming connection from: {socket.RemoteEndPoint}");

        while (socket.Connected)
        {
            try
            {
                stream.ReadExactly(header.AsSpan());

                if (ValidateHeader(header))
                {
                    var payload = new byte[header.dataLength];
                    stream.ReadExactly(payload, 0, header.dataLength);
                    packets.Enqueue(new(header, payload));
                }
                else
                {
                    Log.Warning($"Invalid packet received from {socket.RemoteEndPoint}");
                }
            }
            catch(EndOfStreamException)
            {
                Log.Error($"Reached end of stream while reading packet from {socket.RemoteEndPoint}");
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading packet from {socket.RemoteEndPoint}:\n{ex}");
            }

        }

        Log.Info($"Connection closed: {socket.RemoteEndPoint}");
    }

    private static bool ValidateHeader(in TincatHeader header)
    {
        return true;
    }

    public void HandlePacket(TincatPacket packet)
    {
        var type = packet.Header.msgType;
        switch (type)
        {
            case TincatMsgType.LOGMEON:
                HandleLogMeOn(packet);
                break;

            case TincatMsgType.CUSTOMDATA:
                HandleCustomData(packet);
                break;

            case TincatMsgType.STAYINGALIVE:
                break;

            default:
                if(Enum.IsDefined(packet.Header.msgType))
                    Log.Warning($"Unhandled tincat packet {type}");
                else
                    Log.Warning($"Unhandled tincat packet {(int)type}");
                break;
        }
    }

    private void HandleLogMeOn(TincatPacket packet)
    {
        Utils.FromSpan(packet.Payload, out LogMeOn logMeOn);
        Utils.FromSpan(packet.Payload, out LogOnAccepted logOnAccepted);

        logOnAccepted.connId = connectionId;
        logOnAccepted.unknown = 420;
        logOnAccepted.SetPassword(new byte[] { 0x2D, 0, 0, 0, 0, 0, 0, 0 });

        SendPacket(TincatMsgType.LOGONACCEPTED, logOnAccepted.AsSpan());
    }

    private void HandleCustomData(TincatPacket packet)
    {
        Utils.FromSpan(packet.Payload.AsSpan(0, SacredHeader.HeaderSize), out SacredHeader header);
        var payload = packet.Payload.AsSpan(SacredHeader.HeaderSize);

        ReplyOk(header.type1);

        switch (header.type1)
        {
            case 2:
                {
                    var ms = new MemoryStream();
                    var response = new BinaryWriter(ms);

                    response.Write(0);
                    for (int i = 0; i < 63; i++)
                    {
                        response.Write(connectionId);
                    }
                    SendSacredPacket(16, ms.ToArray());

                    Log.Info("New client logged in!");
                }
                break;

            case 12:
                {
                    Utils.FromSpan(payload, out serverInfo);
                    serverInfo.serverId = (int)connectionId;

                    var ms = new MemoryStream();
                    var response = new BinaryWriter(ms);
                    var ep = socket.RemoteEndPoint as IPEndPoint;
                    var ip = ep!.Address.GetAddressBytes();

                    if(Utils.IsInternal(ep.Address))
                    {
                        using var cl = new HttpClient();
                        var str = cl.GetStringAsync("http://icanhazip.com").Result;
                        ip = IPAddress.Parse(str.AsSpan().Trim('\n')).GetAddressBytes();
                    }

                    Log.Info($"New GameServer connected #{serverInfo.serverId} \"{serverInfo.GetName()}\" with ip {new IPAddress(ip)} port {serverInfo.port}");

                    response.Write(ip);

                    isServer = true;
                    SendSacredPacket(38, ms.ToArray());
                }
                break;

            case 13:
                {
                    Utils.FromSpan(payload, out ServerInfo newInfo);
                    serverInfo.flags = newInfo.flags;
                    serverInfo.maxPlayers = newInfo.maxPlayers;
                    serverInfo.currentPlayers = newInfo.currentPlayers;

                    Log.Info($"GameServer #{serverInfo.serverId} \"{serverInfo.GetName()}\" changed public info");


                    foreach (var client in LobbyServer.clients.Where(x => x.isServer == false))
                    {
                        client.SendSacredPacket(12, serverInfo.AsSpan(), unknown1: 0x12BBCCDD, tincatUnknown: packet.Header.unknown);
                    }
                }
                break;

            case 17:
                {
                    var ms = new MemoryStream();
                    var response = new BinaryWriter(ms);

                    response.Write(0);
                    //Join Channel #0
                    SendSacredPacket(26, ms.ToArray());

                    foreach (var client in LobbyServer.clients.Where(x => x.isServer == true))
                    {
                        var info = new ServerInfo()
                        {
                            currentPlayers = client.serverInfo.currentPlayers,
                            maxPlayers = client.serverInfo.maxPlayers,
                            flags = client.serverInfo.flags,
                            ipAddress = client.serverInfo.ipAddress,
                            port = client.serverInfo.port,
                            version = 0xDCB8,
                            serverId = (int)client.connectionId

                        };
                        info.SetName(client.serverInfo.GetName());
                        SendSacredPacket(12, info.AsSpan(), unknown1: 0x12BBCCDD, tincatUnknown: packet.Header.unknown);
                    }

                }
                break;

            default:
                Log.Warning($"Unhandled packet type {header.type1}");
                break;
        }


    }

    private void ReplyOk(int type, int value = 0)
    {
        var ms = new MemoryStream();
        var response = new BinaryWriter(ms);

        //LobbyResult
        response.Write(value);
        //Message type we're answering to
        response.Write(type);

        //Send an OK
        SendSacredPacket(15, ms.ToArray());

    }

    private void SendPacket(TincatMsgType type, ReadOnlySpan<byte> payload, uint unknown = 0)
    {
        TincatHeader header = new()
        {
            magic = TincatHeader.TincatMagic,
            from = 0xEFFFFFCC,
            to = connectionId,
            msgType = type,
            unknown = unknown,
            dataLength = payload.Length,
            crc32 = CRC32.Compute(payload)
        };

        stream.Write(header.AsSpan());
        stream.Write(payload);
    }

    private void SendSacredPacket(ushort type, ReadOnlySpan<byte> payload, ushort type2 = 0, uint unknown1 = 0xDDCCBB00, uint unknown2 = 0, uint tincatUnknown = 0)
    {
        Span<byte> sacredData = stackalloc byte[SacredHeader.HeaderSize + payload.Length];

        SacredHeader sacredHeader = new()
        {
            magic = SacredHeader.SacredMagic,
            dataLength = payload.Length + SacredHeader.HeaderSize - 4, //I don't understand WHY -4, but it's like that...
            type1 = type,
            type2 = type2 != 0 ? type2 : type,
            unknown1 = unknown1,
            unknown2 = unknown2
        };

        var span = sacredHeader.AsSpan();

        for (int i = 0; i < SacredHeader.HeaderSize; i++)
            sacredData[i] = span[i];

        for (int i = 0; i < payload.Length; i++)
            sacredData[i + SacredHeader.HeaderSize] = payload[i];

        SendPacket(TincatMsgType.CUSTOMDATA, sacredData);
    }
}
