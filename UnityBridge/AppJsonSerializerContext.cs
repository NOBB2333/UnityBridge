using System.Text.Json.Serialization;
using UnityBridge.Api.Dify.Models;

namespace UnityBridge;

/// <summary>
/// JSON Source Generation Context for Native AOT support
/// </summary>
[JsonSerializable(typeof(ConsoleApiAppsRequest))]
[JsonSerializable(typeof(ConsoleApiAppsResponse))]
[JsonSerializable(typeof(ConsoleApiAppsAppidRequest))]
[JsonSerializable(typeof(ConsoleApiAppsAppidResponse))]
[JsonSerializable(typeof(ConsoleApiAppsAppidExportRequest))]
[JsonSerializable(typeof(ConsoleApiAppsAppidExportResponse))]
[JsonSerializable(typeof(ConsoleApiAppsImportsRequest))]
[JsonSerializable(typeof(ConsoleApiAppsImportsResponse))]
[JsonSerializable(typeof(ConsoleApiAppsAppidApikeysRequest))]
[JsonSerializable(typeof(ConsoleApiAppsAppidApikeysResponse))]
[JsonSerializable(typeof(ConsoleApiAppsAppidApikeysCreateRequest))]
[JsonSerializable(typeof(ConsoleApiAppsAppidApikeysCreateResponse))]
[JsonSerializable(typeof(ConsoleApiAppsAppidApikeysKeyidRequest))]
[JsonSerializable(typeof(ConsoleApiAppsAppidApikeysKeyidResponse))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
