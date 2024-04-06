using Sacred.Networking.Types;

namespace Sacred.Networking;

public record TincatPacket(TincatHeader Header, byte[] Payload)
{
    
}
