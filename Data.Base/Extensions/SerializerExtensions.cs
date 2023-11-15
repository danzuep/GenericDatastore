using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Data.Base.Extensions;

[ExcludeFromCodeCoverage]
public static class SerializerExtensions
{
    private static readonly string _nl = Environment.NewLine;

    internal static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    public static string ToSerializedString(this object? obj) =>
        obj != null ? JsonSerializer.Serialize(obj, _serializerOptions) : string.Empty;

    public static string ToJson(this object obj, string? name = null) =>
        string.IsNullOrEmpty(name) ? obj.ToSerializedString() : $"{{{_nl}\"{name}\": {obj.ToSerializedString()}{_nl}}}";

    public static string ToEnumeratedString<T>(this IEnumerable<T> data, string delimiter = ", ") =>
        data is null ? string.Empty : string.Join(delimiter, data.Select(o => o?.ToString() ?? string.Empty));

    public static string ToParameters(this IDictionary<string, string> keyValuePairs, string delimiter = "&", string divider = "=") =>
        keyValuePairs is null ? string.Empty : string.Join(delimiter, keyValuePairs.Select(kvp => $"{kvp.Key}{divider}{kvp.Value}"));
}
