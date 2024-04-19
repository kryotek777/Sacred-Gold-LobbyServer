using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Sacred.Networking.Structs;

namespace Sacred.Networking.Types;

public class ServerInfo
{
    public const int DataSize = ServerInfoData.DataSize;
    public ServerInfoData Data;

    public string Name
    {
        get
        {
            unsafe
            {
                fixed (byte* p = Data.name)
                {
                    var span = new ReadOnlySpan<byte>(p, 24);
                    return Utils.Win1252ToString(span);
                }
            }
        }

        set
        {
            unsafe
            {
                fixed (byte* p = Data.name)
                {
                    var span = new Span<byte>(p, 24);
                    Utils.StringToWin1252(value, span);
                }
            }
        }
    }

    public IPAddress IpAddress
    {
        get => new IPAddress(Data.ipAddress);
        set => Data.ipAddress = value.ToInt();
    }

    public int Port
    {
        get => Data.port;
        set => Data.port = value;
    }

    public short CurrentPlayers
    {
        get => Data.currentPlayers;
        set => Data.currentPlayers = value;
    }

    public short MaxPlayers
    {
        get => Data.maxPlayers;
        set => Data.maxPlayers = value;
    }

    public int Flags
    {
        get => Data.flags;
        set => Data.flags = value;
    }

    public uint ServerId
    {
        get => Data.serverId;
        set => Data.serverId = value;
    }

    public int Version
    {
        get => Data.version;
        set => Data.version = value;
    }

    public int Hidden
    {
        get => Data.hidden;
        set => Data.hidden = value;
    }


    public ServerInfo(in ServerInfoData data)
    {
        Data = data;
    }

    public ServerInfo(ReadOnlySpan<byte> data)
    {
        Data = MemoryMarshal.Read<ServerInfoData>(data);
    }

    public byte[] ToArray()
    {
        unsafe
        {
            fixed (ServerInfoData* data = &Data)
            {
                var arr = new byte[DataSize];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = data->rawData[i];
                }
                return arr;
            }
        }
    }

    public override string ToString()
    {
        return
            $"Name: {Name}\n" +
            $"IP Address: {IpAddress}:{Port}\n" +
            $"Players: {CurrentPlayers}/{MaxPlayers}\n" +
            $"Flags: {Flags:X}\n" +
            $"Server ID: {ServerId}\n" +
            $"Version: {Version:X}\n" +
            $"Hidden: {Hidden}\n";
    }
}