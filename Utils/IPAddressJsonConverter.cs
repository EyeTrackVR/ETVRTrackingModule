using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ETVRTrackingModule.Utils;

public class IPAddressJsonConverter : JsonConverter<IPAddress>
{
    public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readerValue = reader.GetString();
        return readerValue is null ? IPAddress.Loopback : IPAddress.Parse((string)readerValue);
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}