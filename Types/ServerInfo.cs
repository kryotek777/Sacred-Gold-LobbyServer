using System.Runtime.InteropServices;
using System.Text;

namespace Sacred.Types;

[StructLayout(LayoutKind.Explicit, Size = StructSize)]
public unsafe struct ServerInfo
{
    public const int StructSize = 0x74;

    [FieldOffset(0)]
    public fixed byte name[24];
    [FieldOffset(84)]
    public int ipAddress;
    [FieldOffset(88)]
    public int port;
    [FieldOffset(92)]
    public short currentPlayers;
    [FieldOffset(94)]
    public short maxPlayers;
    [FieldOffset(96)]
    public int flags;
    [FieldOffset(100)]
    public int serverId;
    [FieldOffset(108)]
    public int version;
    [FieldOffset(112)]
    public int hidden;

    [FieldOffset(0)]
    public fixed byte rawData[StructSize];

    public string GetName()
    {
        fixed (byte* p = name)
        {
            var i = new ReadOnlySpan<byte>(p, 24).IndexOf((byte)0);
            var s = Encoding.ASCII.GetString(p, i > 0 ? i : 24);
            return s;
        }
    }

    public void SetName(string newName)
    {
        fixed (byte* p = name)
        {
            Encoding.ASCII.GetBytes(newName, new Span<byte>(p, 23));
            p[23] = 0;
        }
    }
    public Span<byte> AsSpan()
    {
        fixed (byte* p = rawData)
            return new(p, StructSize);
    }
}
