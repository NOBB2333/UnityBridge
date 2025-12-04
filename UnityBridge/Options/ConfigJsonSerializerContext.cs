using System.Text.Json.Serialization;

namespace UnityBridge.Options;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(EndpointOptions))]
[JsonSerializable(typeof(DifyMigrationOptions))]
[JsonSerializable(typeof(SionWebAppOptions))]
public partial class ConfigJsonSerializerContext : JsonSerializerContext
{
}

