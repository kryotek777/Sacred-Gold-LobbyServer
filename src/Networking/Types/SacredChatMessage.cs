using System.Runtime.InteropServices;
using System.Text;
using Sacred.Networking.Structs;

namespace Sacred.Networking.Types;

public class SacredChatMessage
{
    public const int DataSize = SacredChatMessageData.DataSize;

    public SacredChatMessageData Data;

    public string From
    {
        get
        {
            unsafe
            {
                fixed (byte* p = Data.senderText)
                {
                    var span = new ReadOnlySpan<byte>(p, 90);
                    return Encoding.ASCII.GetString(span.SliceNullTerminated());
                }
            }
        }

        set
        {
            unsafe
            {
                fixed (byte* p = Data.senderText)
                {
                    var span = new Span<byte>(p, 90);
                    var bytes = Encoding.ASCII.GetBytes(value, span);

                    if (bytes < span.Length)
                        span.Slice(bytes).Clear();
                }
            }
        }
    }

    public bool IsPrivate
    {
        get => Data.isPrivateMessage != 0;
        set => Data.isPrivateMessage = value ? 1 : 0;
    }

    public uint SenderId 
    {
        get => Data.senderId;
        set => Data.senderId = value;
    }

    public string Message
    {
        get
        {
            unsafe
            {
                fixed (byte* p = Data.messageText)
                {
                    var span = new ReadOnlySpan<byte>(p, 128);
                    return Encoding.ASCII.GetString(span.SliceNullTerminated());
                }
            }
        }

        set
        {
            unsafe
            {
                fixed (byte* p = Data.messageText)
                {
                    var span = new Span<byte>(p, 128);
                    var bytes = Encoding.ASCII.GetBytes(value, span);

                    if (bytes < span.Length)
                        span.Slice(bytes).Clear();
                }
            }
        }
    }

    public SacredChatMessage(string from, string message, uint senderId, bool isPrivate)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(message);
        From = from;
        Message = message;
        SenderId = senderId;
        IsPrivate = isPrivate;
    }


    public SacredChatMessage(in SacredChatMessageData data)
    {
        Data = data;
    }

    public SacredChatMessage(ReadOnlySpan<byte> data)
    {
        Data = MemoryMarshal.Read<SacredChatMessageData>(data);
    }

    public byte[] ToArray()
    {
        unsafe
        {
            fixed (SacredChatMessageData* data = &Data)
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

    public override string ToString() => 
    $"From: '{From}'\n" +
    $"Text: '{Message}\n'" +
    $"Private: {IsPrivate}\n"+
    $"SenderId: {SenderId}\n";
}