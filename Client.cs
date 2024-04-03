using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
                //Read the header
                stream.ReadExactly(header.AsSpan());

                //Reject packets with invalid Magic Number
                if (header.magic == TincatHeader.TincatMagic)
                {
                    //Read the payload
                    var payload = new byte[header.dataLength];
                    stream.ReadExactly(payload, 0, header.dataLength);

                    //Reject packets with invalid CRC32
                    var crc32 = CRC32.Compute(payload);
                    if (crc32 == header.crc32)
                    {
                        packets.Enqueue(new(header, payload));
                    }
                    else
                    {
                        Log.Warning($"CRC32 Mismatch in Tincat Packet received from {socket.RemoteEndPoint}: Expected 0x{header.crc32} but got 0x{crc32}");
                    }
                }
                else
                {
                    Log.Warning($"Invalid Tincat Magic received from {socket.RemoteEndPoint}: Expected 0x{TincatHeader.TincatMagic:X}) but got 0x{header.magic:X}");
                }
            }
            catch (EndOfStreamException)
            {
                //Connection has been closed
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading packet from {socket.RemoteEndPoint}:\n{ex}");
            }
        }

        Log.Info($"Connection closed: {socket.RemoteEndPoint}");

        LobbyServer.clients.Remove(this);
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
                if (Enum.IsDefined(packet.Header.msgType))
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

        ReplyOk((int)header.type1);

        switch (header.type1)
        {
            case SacredMsgType.ClientLoginRequest:
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

            case SacredMsgType.ServerLoginRequest:
                HandleServerLoginRequest(packet.Header, header, payload);
                break;

            case SacredMsgType.ServerChangePublicInfo:
                {
                    Utils.FromSpan(payload, out ServerInfo newInfo);
                    serverInfo.flags = newInfo.flags;
                    serverInfo.maxPlayers = newInfo.maxPlayers;
                    serverInfo.currentPlayers = newInfo.currentPlayers;

                    Log.Info($"GameServer #{serverInfo.serverId} \"{serverInfo.GetName()}\" changed public info");


                    foreach (var client in LobbyServer.clients.Where(x => x.isServer == false))
                    {
                        var info = client.serverInfo;
                        info.hidden = 0;
                        client.SendSacredPacket(12, serverInfo.AsSpan(), unknown1: 0x12BBCCDD, tincatUnknown: packet.Header.unknown);
                    }
                }
                break;

            case SacredMsgType.ClientCharacterSelect:
                HandleCharacterSelect(packet.Header, header, payload);
                break;

            case SacredMsgType.ClientChatMessage:
                {
                    DumpPacket(packet.Header, header, payload);
                    Utils.FromSpan<SacredChatMessageData>(payload, out var data);
                    HandleChatMessage(packet.Header, header, data);
                    break;
                }

            default:
                Log.Warning($"Unhandled packet type {(int)header.type1} from {socket.RemoteEndPoint}");
                DumpPacket(packet.Header, header, payload);
                break;
        }
    }

    private void DumpPacket(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        Log.Trace(FormatPacket(tincatHeader, sacredHeader, payload));
    }

    private string FormatPacket(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Dumping packet:");
        sb.AppendLine("---Tincat Header---");
        sb.AppendLine($"magic: {tincatHeader.magic:X}");
        sb.AppendLine($"from: {tincatHeader.from:X}");
        sb.AppendLine($"to: {tincatHeader.to:X}");
        sb.AppendLine($"msgType: {tincatHeader.msgType}");
        sb.AppendLine($"unknown: {tincatHeader.unknown:X}");
        sb.AppendLine($"dataLength: {tincatHeader.dataLength}");
        sb.AppendLine($"crc32: {tincatHeader.crc32:X}");
        sb.AppendLine();
        sb.AppendLine("---Sacred Header---");
        sb.AppendLine($"magic: {sacredHeader.magic:X}");
        sb.AppendLine($"type1: {sacredHeader.type1}");
        sb.AppendLine($"type2: {sacredHeader.type2}");
        sb.AppendLine($"unknown1: {sacredHeader.unknown1:X}");
        sb.AppendLine($"dataLength: {sacredHeader.dataLength}");
        sb.AppendLine($"unknown2: {sacredHeader.unknown2:X}");
        sb.AppendLine();

        FormatBytes(payload, sb);

        return sb.ToString();
    }

    private void FormatBytes(ReadOnlySpan<byte> data, StringBuilder sb)
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

    private string FormatBytes(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder();
        FormatBytes(data, sb);
        return sb.ToString();
    }

    private void HandleServerLoginRequest(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {   
        //This client is a GameServer, we need to save it's info 
        isServer = true;
        Utils.FromSpan(payload, out serverInfo);
        serverInfo.serverId = (int)connectionId;

        //Get the public ip address of the server
        var ep = socket.RemoteEndPoint as IPEndPoint;
        var ip = ep!.Address.GetAddressBytes();

        if (Utils.IsInternal(ep.Address))
        {
            using var cl = new HttpClient();
            var str = cl.GetStringAsync("http://icanhazip.com").Result;
            ip = IPAddress.Parse(str.AsSpan().Trim('\n')).GetAddressBytes();
        }

        Log.Info($"New GameServer connected #{serverInfo.serverId} \"{serverInfo.GetName()}\" with ip {new IPAddress(ip)} port {serverInfo.port}");

        SendSacredPacket(38, ip);
    }

    private void HandleCharacterSelect(TincatHeader tincatHeader, SacredHeader sacredHeader, ReadOnlySpan<byte> payload)
    {
        //Immediately join Room #0
        //Rooms aren't implemented yet, but if we force the right answer the client will happily join
        JoinRoom(0);

        //Send the server list to the client
        foreach (var client in LobbyServer.clients.Where(x => x.isServer == true))
        {
            var info = client.serverInfo;
            info.hidden = 0;

            SendSacredPacket(12, info.AsSpan(), unknown1: 0x12BBCCDD, tincatUnknown: tincatHeader.unknown);
        }
    }

    private void HandleChatMessage(TincatHeader tincatHeader, SacredHeader sacredHeader, SacredChatMessageData message)
    {
        var msg = message.GetMessageString();

        Log.Info($"Client {socket.RemoteEndPoint} sent a chat message: {msg}");
    }

    private void JoinRoom(int roomNumber)
    {
        SendSacredPacket(26, BitConverter.GetBytes(roomNumber));
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
            type1 = (SacredMsgType)(type),
            type2 = (SacredMsgType)(type2 != 0 ? type2 : type),
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
