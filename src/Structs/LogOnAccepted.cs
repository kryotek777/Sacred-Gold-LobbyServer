using System.Runtime.InteropServices;

namespace Sacred.Types;

[StructLayout(LayoutKind.Explicit, Size = StructSize)]
public unsafe struct LogOnAccepted
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

    public void SetPassword(byte[] array)
    {
        fixed (byte* p = password)
            for (int i = 0; i < 8; i++)
                p[i] = array[i];
    }


    public ReadOnlySpan<byte> AsSpan()
    {
        fixed (byte* p = rawData)
            return new ReadOnlySpan<byte>(p, StructSize);
    }
}
