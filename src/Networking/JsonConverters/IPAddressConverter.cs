using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

public class IPAddressConverter : JsonConverter<IPAddress>
{
    public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string ipAddressString = reader.GetString();
        return IPAddress.Parse(ipAddressString);
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}