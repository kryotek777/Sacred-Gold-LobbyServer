using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Lobby.Types;

namespace Lobby.Networking;

/// <summary>
/// Handles all the networking logistics for a SacredClient.
/// </summary>
public class SacredConnection
{
    private const uint TincatMagic = 0xDABAFBEF;
    private const uint ServerId = 0xEFFFFFCC;
    private const ushort ModuleId = 9910;
    private const int TincatHeaderSize = 28;


    /// <summary>
    /// The SacredClient that owns this connection.
    /// </summary>
    private SacredClient Client { get; init; }

    /// <summary>
    /// The ConnectionId for our TinCat communication.
    /// </summary>
    private uint ConnectionId { get; init; }

    /// <summary>
    /// An already open socket connection our client.
    /// </summary>
    private Socket Socket { get; init; }

    /// <summary>
    /// Data stream to our client.
    /// </summary>
    private NetworkStream Stream { get; init; }

    [MemberNotNullWhen(true, nameof(ReadTask))]
    [MemberNotNullWhen(true, nameof(WriteTask))]
    private bool Started { get; set; }
    private Task? ReadTask { get; set; }
    private Task? WriteTask { get; set; }
    private CancellationTokenSource CancellationTokenSource { get; init; }

    private Channel<SacredPacket> SendQueue { get; init; }


    /// <summary>
    /// Represents what type of client we're dealing with (GameClient, GameServer or we don't know yet). 
    /// Used for packet filtering.
    /// </summary>
    public ClientType ClientType { get; set; }

    public bool IsConnected => Socket.Connected;
    public IPEndPoint RemoteEndPoint => (IPEndPoint)Socket.RemoteEndPoint!;

    /// <summary>
    /// Creates a new SacredConnection
    /// </summary>
    /// <param name="client">The SacredClient that owns this connection</param>
    /// <param name="socket">An already connected socket to our client</param>
    /// <param name="connectionId">The LobbyServer provided id for TinCat communication with the client</param>
    public SacredConnection(SacredClient client, Socket socket, uint connectionId, CancellationToken parentToken)
    {
        Client = client;
        ConnectionId = connectionId;
        Socket = socket;
        ClientType = ClientType.Unknown;
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        SendQueue = Channel.CreateUnbounded<SacredPacket>();

        Stream = new(socket);
        ReadTask = null;
        WriteTask = null;
        Started = false;
    }

    public void Start()
    {
        var token = CancellationTokenSource.Token;
        ReadTask = ReadLoop(token);
        WriteTask = WriteLoop(token);
        Started = true;
    }

    public void Stop()
    {
        if (Started)
        {
            Started = false;
            CancellationTokenSource.Cancel();
            SendQueue.Writer.Complete();
            Client.Stop();
        }
    }

    public void EnqueuePacket(SacredMsgType type, byte[] data)
    {
        try
        {
            var written = SendQueue.Writer.TryWrite(new SacredPacket(Client, type, data));

            if(!written && !CancellationTokenSource.Token.IsCancellationRequested && IsConnected)
            {
                Log.Error($"Failed to write to the send queue for client {Client.ClientName}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception while reading packet: {ex}");
            Stop();
        }
    }

    private async Task ReadLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && Socket.Connected)
            {
                await ReadPacket(token);
                Statistics.IncrementPacketsReceived();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (EndOfStreamException)
        {
            Stop();
        }
        catch (IOException)
        {
            Stop();
        }
        catch (Exception ex)
        {
            Log.Error($"Exception while reading packet: {ex}");
            Stop();
        }
    }

    private async Task WriteLoop(CancellationToken token)
    {
        try
        {
            await foreach (var packet in SendQueue.Reader.ReadAllAsync(token))
            {
                SendSacredPacket(packet.Type, packet.Payload);
                Statistics.IncrementPacketsSent();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (EndOfStreamException)
        {
            Stop();
        }
        catch (IOException)
        {
            Stop();
        }
        catch (Exception ex)
        {
            Log.Error($"Exception while writing packet: {ex}");
            Stop();
        }
    }

    private async Task ReadPacket(CancellationToken token)
    {
        // Read the TinCat header
        var headerData = await Read(TincatHeaderSize, token);
        TincatHeader.Deserialize(headerData, out var header);

        // Not a TinCat header
        if (header.Magic != TincatMagic)
        {
            Log.Error($"{Client.ClientName}: Wrong magic number {header.Magic:X}, not a TinCat packet?");
            return;
        }

        // Read the payload
        var payloadData = await Read(header.Length, token);

        // Check the payload's integrity
        var checksum = CRC32.Compute(payloadData);

        if (header.Checksum != checksum)
        {
            Log.Error($"{Client.ClientName}: CRC32 check failed!");
            return;
        }

        // Dispatch the packet
        switch (header.Type)
        {
            case TincatMsgType.CUSTOMDATA:
                OnCustomData(payloadData);
                break;
            case TincatMsgType.LOGMEON:
                OnLogMeOn(LogOn.Deserialize(payloadData));
                break;
            case TincatMsgType.LOGMEOFF:
                OnLogMeOff();
                break;

            // Ignore timesync/keepalive
            case TincatMsgType.TIMESYNC:
            case TincatMsgType.STAYINGALIVE:
                break;

            default:
                {
                    Log.Error($"{Client.ClientName}: Unexpected TinCat packet {(int)header.Type:X}!");
                    break;
                }
        }

    }
    
    private async ValueTask<byte[]> Read(int length, CancellationToken token)
    {
        Debug.Assert(length > 0);
        var buffer = new byte[length];
        await Stream.ReadExactlyAsync(buffer, token);
        Statistics.AddBytesReceived((ulong)buffer.Length);
        return buffer;
    }

    private void Write(ReadOnlySpan<byte> data)
    {
        Stream.Write(data);
        Statistics.AddBytesSent((ulong)data.Length);
    }

    private void SendTincatPacket(TincatMsgType type, ReadOnlySpan<byte> data)
    {
        var header = new TincatHeader(
            Magic: TincatMagic,
            Source: ServerId,
            Destination: ConnectionId,
            Type: type,
            Unknown: 0,
            Length: data.Length,
            Checksum: CRC32.Compute(data)
        );

        var headerData = new byte[TincatHeaderSize];
        var writer = new SpanWriter(headerData);
        header.Serialize(ref writer);

        Write(headerData);
        Write(data);
    }

    private void SendSacredPacket(SacredMsgType type, byte[] data)
    {
        SpanWriter tincatPayload = stackalloc byte[18 + data.Length];

        tincatPayload.Write(ModuleId);
        tincatPayload.Write((ushort)type);

        var securityData = PacketSecurityData.Get(type);
        var securityHeader = new PacketSecurityHeader(type, securityData.SecurityKey, data.Length + 14, 0);

        securityHeader.Serialize(ref tincatPayload);

        tincatPayload.Write(data);

        SendTincatPacket(TincatMsgType.CUSTOMDATA, tincatPayload.Span);
    }

    private void OnLogMeOn(LogOn logOn)
    {
        if (
            logOn.Magic == LogOn.LogOnMagic &&
            logOn.ConnectionId == LogOn.LogOnConnId &&
            logOn.Username == "user" &&
            logOn.Password == "passwor"
        )
        {
            var response = new LogOn(Client.ConnectionId);

            Span<byte> data = stackalloc byte[52];
            response.Serialize(data);

            SendTincatPacket(TincatMsgType.LOGONACCEPTED, data);
        }
    }

    private void OnLogMeOff()
    {
        Stop();
    }


    private void OnCustomData(ReadOnlySpan<byte> data)
    {
        var reader = new SpanReader(data);
        var moduleId = reader.ReadUInt16();
        var type = (SacredMsgType)reader.ReadInt16();
        var securityHeader = PacketSecurityHeader.Deserialize(ref reader);

        if (!Config.Instance.SkipSecurityChecks)
        {

            // Wrong module id
            if (moduleId != ModuleId)
            {
                Log.Error($"{Client.ClientName}: Wrong module id {moduleId:X}!");
                return;
            }

            // TODO: Checksum
            // I'm pretty sure it's a checksum, but there's something weird going on
            // For now we're gonna skip it, I haven't seen a single corrupted packet yet
            // And we're covered by the TinCat's checksum too

            // Check the security data

            // Is the reduntant type the same?
            if (securityHeader.Type != type)
            {
                Log.Error($"{Client.ClientName}: Types don't match, packet is corrupt");
                return;
            }

            var securityData = PacketSecurityData.Get(type);

            // Have we got the right security key?
            if (securityHeader.SecurityKey != securityData.SecurityKey)
            {
                Log.Error($"{Client.ClientName}: Security key is wrong!");
                return;
            }

            // Is this packet allowed from this connection type?
            if (!securityData.AllowedClients.HasFlag(ClientType))
            {
                Log.Error($"{Client.ClientName}: Packet {type} not allowed from this client of type {ClientType}!");
                return;
            }

            // Is the packet the right length?
            if (!securityData.DynamicSize && securityData.Length != reader.Span.Length - 4)
            {
                Log.Error($"{Client.ClientName}: Packet length is wrong!");
                return;
            }

        }

        // Packet is valid, dispatch it's payload

        var payload = reader.ReadAll().ToArray();

        var packet = new SacredPacket(Client, type, payload);

        LobbyServer.ReceivePacket(packet);

        return;
    }
}
