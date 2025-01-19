using System.Data.SQLite;

namespace Lobby.DB;

public static class SQLiteExtensions
{
    public static T GetWithName<T>(this SQLiteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        var value = reader.GetValue(ordinal);
        var type = reader.GetFieldType(ordinal);
        
        if(type == typeof(T))
        {
            return (T)value;
        }
        else
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }

    public static byte[] GetBlobValue(this SQLiteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        var blob = reader.GetBlob(ordinal, true);
        var length = blob.GetCount();
        var buffer = new byte[length];
        blob.Read(buffer, length, 0);
        return buffer;
    }
}