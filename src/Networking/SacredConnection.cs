using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Sacred.Networking.Structs;
using Sacred.Networking.Types;

namespace Sacred.Networking;

public class SacredConnection
{
    private Socket socket;

    private NetworkStream stream;

    public bool IsConnected => socket.Connected;
    public IPEndPoint RemoteEndPoint => (IPEndPoint)socket.RemoteEndPoint!;

    public SacredConnection(Socket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        this.socket = socket;
        stream = new(socket);
    }

    public bool TryReadPacket(out TincatPacket? packet, out PacketError error)
    {
        TincatHeaderData headerData = default;
        packet = null;

        //Get a Span<byte> that points to headerData
        //This is safe because we're pointing into the current stack
        var span = MemoryMarshal.Cast<TincatHeaderData, byte>(MemoryMarshal.CreateSpan(ref headerData, 1));

        //Read only the magic number
        stream.ReadExactly(span.Slice(0, 4));

        //Reject early if magic is wrong
        if (headerData.magic != TincatHeader.TincatMagic)
        {
            error = PacketError.WrongMagic;
            return false;
        }

        //Read the rest of the header
        stream.ReadExactly(span.Slice(4));

        //Read the payload and compute the checksum
        var payload = new byte[headerData.payloadLength];
        stream.ReadExactly(payload);
        var checksum = CRC32.Compute(payload);

        //Reject malformed packets
        if (checksum != headerData.checksum)
        {
            error = PacketError.WrongChecksum;
            return false;
        }

        //All good, create the actual packet
        packet = new TincatPacket(new TincatHeader(headerData), payload);
        error = PacketError.None;
        return true;
    }

    public void SendPacket(TincatPacket packet)
    {
        unsafe
        {
            //The header data needs to be fixed because it's wrapped into the heap
            fixed (byte* p = packet.Header.Data.rawData)
            {
                var span = new ReadOnlySpan<byte>(p, TincatHeader.DataSize);
                stream.Write(span);
            }
        }

        stream.Write(packet.Payload);
    }
}
