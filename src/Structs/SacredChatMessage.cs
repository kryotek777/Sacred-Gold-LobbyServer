using System.Runtime.InteropServices;
using System.Text;

namespace Sacred;

[StructLayout(LayoutKind.Explicit, Size = StructSize)]
internal unsafe struct SacredChatMessageData
{
    public const int StructSize = 344;

    [FieldOffset(88)]
    public fixed byte messageBytes[128];

    public string GetMessageString()
    {
        fixed(byte* b = messageBytes)
            return Encoding.ASCII.GetString(b, 128);
    }
} 