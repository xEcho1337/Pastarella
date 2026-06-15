using System.Text.Json;
using System.Text.Json.Serialization;
using Pastarella.Core;
using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs;

public static class JsonWriter
{
    public static string Serialize(AnalysisReport report)
    {

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new PortInfoConverter());

        return JsonSerializer.Serialize(report, options);
    }
}
