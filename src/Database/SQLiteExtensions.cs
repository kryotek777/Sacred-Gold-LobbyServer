using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;

namespace Lobby.DB;

public static class SQLiteExtensions
{
    public static bool TryGetWithName<T>(this SQLiteDataReader reader, string name, [NotNullWhen(true)] out T? value)
    {
        var ordinal = reader.GetOrdinal(name);
        var rawValue = reader.GetValue(ordinal);
        var type = reader.GetFieldType(ordinal);
        
        if(rawValue is DBNull)
        {
            value = default;
            return false;
        }
        else if(type == typeof(T))
        {
            value = (T)rawValue;
            return true;
        }
        else
        {
            value = (T)Convert.ChangeType(rawValue, typeof(T));
            return true;
        }
    }

    public static T GetWithName<T>(this SQLiteDataReader reader, string name)
    {
        if(reader.TryGetWithName<T>(name, out var value))
            return value;
        else
            throw new ArgumentException($"Unexpected NULL from database for column {name}");
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