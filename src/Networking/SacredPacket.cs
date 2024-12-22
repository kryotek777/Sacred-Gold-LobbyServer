using Lobby.Networking.Types;

namespace Lobby.Networking;

public record SacredPacket(SacredClient Sender, SacredMsgType Type, byte[] Payload);