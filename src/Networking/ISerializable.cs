namespace Sacred.Networking.Types;

public interface ISerializable<T> where T : ISerializable<T>
{
    public byte[] Serialize();
    public static abstract T Deserialize(ReadOnlySpan<byte> span);
}