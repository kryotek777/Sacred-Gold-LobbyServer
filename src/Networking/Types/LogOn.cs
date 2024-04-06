using System.Runtime.InteropServices;
using System.Text;
using Sacred.Networking.Structs;

namespace Sacred.Networking.Types;

public class LogOn
{
    public const int DataSize = LogOnData.DataSize;
    public const uint LogOnMagic = 0xDABAFBEF;
    public const uint LogOnConnId = 0xEFFFFFEE;

    public LogOnData Data;

    public uint Magic
    {
        get => Data.magic;
        set => Data.magic = value;
    }

    public uint ConnectionId
    {
        get => Data.connId;
        set => Data.connId = value;
    }

    public string User
    {
        get
        {
            unsafe
            {
                fixed (byte* p = Data.user)
                {
                    var span = new ReadOnlySpan<byte>(p, 32);
                    return Encoding.ASCII.GetString(span.SliceNullTerminated());
                }
            }
        }

        set
        {
            unsafe
            {
                fixed (byte* p = Data.user)
                {
                    var span = new Span<byte>(p, 32);
                    var bytes = Encoding.ASCII.GetBytes(value, span);

                    if (bytes < span.Length)
                        span.Slice(bytes).Clear();
                }
            }
        }
    }

    public string Password
    {
        get
        {
            unsafe
            {
                fixed (byte* p = Data.password)
                {
                    var span = new ReadOnlySpan<byte>(p, 8);
                    return Encoding.ASCII.GetString(span.SliceNullTerminated());
                }
            }
        }

        set
        {
            unsafe
            {
                fixed (byte* p = Data.password)
                {
                    var span = new Span<byte>(p, 8);
                    var bytes = Encoding.ASCII.GetBytes(value, span);

                    if (bytes < span.Length)
                        span.Slice(bytes).Clear();
                }
            }
        }
    }

    public uint Unknown
    {
        get => Data.unknown;
        set => Data.unknown = value;
    }


    public LogOn(uint connectionId)
    {
        Magic = LogOnMagic;
        ConnectionId = connectionId;
        User = "user";
        Password = "-";
        Unknown = 0;
    }

    public LogOn(in LogOnData data)
    {
        Data = data;
    }

    public LogOn(ReadOnlySpan<byte> data)
    {
        Data = MemoryMarshal.Read<LogOnData>(data);
    }

    public byte[] ToArray()
    {
        unsafe
        {
            fixed (LogOnData* data = &Data)
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
            $"Magic: {Magic:X}\n" +
            $"Connection Id: {ConnectionId}\n" +
            $"User: '{User}'\n" +
            $"Password: '{Password}'\n" +
            $"Unknown: {Unknown:X}\n";

    }
}