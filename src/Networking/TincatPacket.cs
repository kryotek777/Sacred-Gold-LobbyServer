using Lobby.Networking.Types;

namespace Lobby.Networking;

public record TincatPacket(TincatHeader Header, byte[] Payload)
{

}
