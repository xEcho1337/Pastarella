using System.Text.Json;
using System.Text.Json.Serialization;
using Pastarella.Core.Models;

namespace Pastarella.Core;

public class PortInfoConverter : JsonConverter<PortInfo>
{
    public override PortInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        string protocol = root.GetProperty("Protocol").GetString() ?? throw new Exception();

        return protocol switch
        {
            "TCP" => JsonSerializer.Deserialize<TcpPortInfo>(root.GetRawText(), options)!,
            "UDP" => JsonSerializer.Deserialize<UdpPortInfo>(root.GetRawText(), options)!,
            _ => throw new JsonException($"Unknown Protocol: {protocol}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PortInfo value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize<object>(writer, value, options);
    }
}
