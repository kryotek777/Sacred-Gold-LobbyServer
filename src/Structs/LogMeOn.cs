using System.Runtime.InteropServices;
using System.Text;

namespace Sacred.Types;

[StructLayout(LayoutKind.Explicit, Size = StructSize)]
public unsafe struct LogMeOn
{
    public const int StructSize = 0x34;

    [FieldOffset(0)]
    public uint magic;
    [FieldOffset(4)]
    public uint connId;
    [FieldOffset(8)]
    public fixed byte user[32];
    [FieldOffset(40)]
    public fixed byte password[8];
    [FieldOffset(48)]
    public uint unknown;

    [FieldOffset(0)]
    public fixed byte rawData[StructSize];

    public string GetUser()
    {
        fixed (byte* p = user)
        {
            var i = new ReadOnlySpan<byte>(p, 32).IndexOf((byte)0);
            var s = Encoding.ASCII.GetString(p, i > 0 ? i : 32);
            return s;
        }
    }

    public string GetPassword()
    {
        fixed (byte* p = password)
        {
            var i = new ReadOnlySpan<byte>(p, 8).IndexOf((byte)0);
            var s = Encoding.ASCII.GetString(p, i > 0 ? i : 8);
            return s;
        }
    }

    public Span<byte> AsSpan()
    {
        fixed (byte* p = rawData)
            return new Span<byte>(p, StructSize);
    }
}
