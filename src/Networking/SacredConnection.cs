using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Lobby.Networking.Types;

namespace Lobby.Networking;

/// <summary>
/// Handles all the networking logistics for a SacredClient.
/// </summary>
public class SacredConnection
{
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

    private BlockingCollection<(SacredMsgType type, byte[] data)> SendQueue { get; init; }


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
        SendQueue = new();

        Stream = new(socket);
        ReadTask = null;
        WriteTask = null;
        Started = false;
    }

    public void Start()
    {
        // Unfortunately Span<T> isn't usable in an async context yet
        // This will run stuff synchronously in a separate thread for now
        var token = CancellationTokenSource.Token;
        ReadTask = Utils.RunTask(ReadLoop, token);
        WriteTask = Utils.RunTask(WriteLoop, token);
        Started = true;
    }

    public void Stop()
    {
        if (Started)
        {
            Started = false;
            CancellationTokenSource.Cancel();
            Client.Stop();
        }
    }

    public void EnqueuePacket(SacredMsgType type, byte[] data)
    {
        SendQueue.Add((type, data));
    }

    private void ReadLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && Socket.Connected)
            {
                var error = ReadPacket();

                if (error != PacketError.None)
                {
                    var message = error switch
                    {
                        PacketError.WrongMagic => "Wrong magic number, not a TinCat packet?",
                        PacketError.WrongChecksum => "CRC32 check failed!",
                        PacketError.PacketUnexpected => "Unexpected TinCat packet",
                        PacketError.WrongModuleId => "Wrong module id!",
                        PacketError.SecurityTypeMismatch => "Types don't match, packet is corrupt",
                        PacketError.SecurityWrongKey => "Security key is wrong!",
                        PacketError.SecurityNotAllowed => "Packet not allowed from this type of client",
                        PacketError.SecurityLengthMismatch => "Packet length is wrong!",
                        _ => "Unknown error"
                    };

                    Log.Error($"{Client.GetPrintableName()}: {message}");
                }
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

    private void WriteLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && Socket.Connected)
            {
                var (type, data) = SendQueue.Take(token);
                SendSacredPacket(type, data);
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

    private PacketError ReadPacket()
    {
        // Read the TinCat header
        Span<byte> headerData = stackalloc byte[Constants.TincatHeaderSize];
        var reader = new SpanReader(headerData);
        Stream.ReadExactly(headerData);
        TincatHeader.Deserialize(ref reader, out var header);

        // Not a TinCat header
        if (header.Magic != Constants.TincatMagic)
        {
            return PacketError.WrongMagic;
        }

        // Read the payload
        Span<byte> payloadData = stackalloc byte[header.Length];
        Stream.ReadExactly(payloadData);

        // Check the payload's integrity
        var checksum = CRC32.Compute(payloadData);

        if (header.Checksum != checksum)
        {
            return PacketError.WrongChecksum;
        }

        // Dispatch the packet
        switch (header.Type)
        {
            case TincatMsgType.CUSTOMDATA:
                return OnCustomData(payloadData);
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
                return PacketError.PacketUnexpected;
        }

        return PacketError.None;
    }

    private void SendTincatPacket(TincatMsgType type, ReadOnlySpan<byte> data)
    {
        var header = new TincatHeader(
            Magic: Constants.TincatMagic,
            Source: Constants.ServerId,
            Destination: ConnectionId,
            Type: type,
            Unknown: 0,
            Length: data.Length,
            Checksum: CRC32.Compute(data)
        );

        header.Serialize(Stream);
        Stream.Write(data);
    }

    private void SendSacredPacket(SacredMsgType type, byte[] data)
    {
        SpanWriter tincatPayload = stackalloc byte[18 + data.Length];

        tincatPayload.Write(Constants.ModuleId);
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


    private PacketError OnCustomData(SpanReader reader)
    {
        var moduleId = reader.ReadUInt16();
        var type = (SacredMsgType)reader.ReadInt16();
        var securityHeader = PacketSecurityHeader.Deserialize(ref reader);

        // Wrong module id
        if (moduleId != Constants.ModuleId)
        {
            return PacketError.WrongModuleId;
        }

        // TODO: Checksum
        // I'm pretty sure it's a checksum, but there's something weird going on
        // For now we're gonna skip it, I haven't seen a single corrupted packet yet
        // And we're covered by the TinCat's checksum too

        // Check the security data

        // Is the reduntant type the same?
        if (securityHeader.Type != type)
        {
            return PacketError.SecurityTypeMismatch;
        }

        var securityData = PacketSecurityData.Get(type);

        // Have we got the right security key?
        if (securityHeader.SecurityKey != securityData.SecurityKey)
        {
            return PacketError.SecurityWrongKey;
        }

        // Is this packet allowed from this connection type?
        if (
            (!securityData.FromClient && ClientType == ClientType.GameClient) ||
            (!securityData.FromServer && ClientType == ClientType.GameServer) ||
            (!securityData.FromUnknown && ClientType == ClientType.Unknown)
        )
        {
            return PacketError.SecurityNotAllowed;
        }

        // Is the packet the right length?
        if (!securityData.DynamicSize && securityData.Length != reader.Span.Length - 4)
        {
            return PacketError.SecurityLengthMismatch;
        }

        // Packet is valid, dispatch it's payload

        var payload = reader.ReadAll();

        Client.ReceivePacket(type, payload);

        return PacketError.None;
    }
}
