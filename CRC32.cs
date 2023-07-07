namespace Sacred;

internal static class CRC32
{
    private static readonly uint[] ChecksumTable;
    private static readonly uint Polynomial = 0xEDB88320;

    static CRC32()
    {
        ChecksumTable = new uint[0x100];

        for (uint index = 0; index < 0x100; ++index)
        {
            uint item = index;
            for (int bit = 0; bit < 8; ++bit)
                item = ((item & 1) != 0) ? (Polynomial ^ (item >> 1)) : (item >> 1);
            ChecksumTable[index] = item;
        }
    }

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint result = 0;

        for (int i = 0; i < data.Length; i++)
        {
            result = ChecksumTable[(result & 0xFF) ^ data[i]] ^ (result >> 8);
        }

        return result;
    }
}
