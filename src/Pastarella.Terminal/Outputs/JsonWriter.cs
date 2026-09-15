using System.Text.Json;
using System.Text.Json.Serialization;
using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs;

public static class JsonWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public static string Serialize(AnalysisReport report)
    {
        Options.Converters.Add(new JsonStringEnumConverter());

        return JsonSerializer.Serialize(report, Options);
    }
}
