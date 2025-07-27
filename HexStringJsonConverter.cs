using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CCDScheduler;

public sealed class HexStringJsonConverter : JsonConverter<nint>
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(nint);
    }

    public override nint Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions serializer)
    {
        return nint.Parse(reader.GetString()!, NumberStyles.HexNumber);
    }

    public override void Write(Utf8JsonWriter writer, nint value, JsonSerializerOptions serializer)
    {
        writer.WriteStringValue($"{value:x}");
    }
}