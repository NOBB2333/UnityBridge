using System.Text.Json.Serialization;
using UnityBridge.Api.Dify.Models;

namespace UnityBridge.Configuration.Options;

/// <summary>
/// json源代码生成AOT支持。
/// Merged: API models + Config options
/// </summary>
// Dify API Models
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
[JsonSerializable(typeof(ConsoleApiAppsAppidWorkflowsPublishRequest))]
[JsonSerializable(typeof(ConsoleApiAppsAppidWorkflowsPublishResponse))]
[JsonSerializable(typeof(ConsoleApiAppsAppidWorkflowsPublishGetRequest))]
[JsonSerializable(typeof(ConsoleApiAppsAppidWorkflowsPublishGetResponse))]
[JsonSerializable(typeof(ConsoleApiAppsResponse.Types.App.WorkflowInfo), TypeInfoPropertyName = "ConsoleApiAppsResponseAppWorkflowInfo")]
[JsonSerializable(typeof(ConsoleApiAppsAppidResponse.WorkflowInfo), TypeInfoPropertyName = "ConsoleApiAppsAppidWorkflowInfo")]
[JsonSerializable(typeof(ConsoleApiAppsAppidResponse.SiteInfo))]
// Workflow Tool API Models
[JsonSerializable(typeof(ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest))]
[JsonSerializable(typeof(ConsoleApiWorkspacesCurrentToolProviderWorkflowGetResponse))]
[JsonSerializable(typeof(ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateRequest))]
[JsonSerializable(typeof(ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateResponse))]
[JsonSerializable(typeof(WorkflowToolIcon))]
[JsonSerializable(typeof(WorkflowToolParameter))]
[JsonSerializable(typeof(List<WorkflowToolParameter>))]
[JsonSerializable(typeof(ConsoleApiWorkspacesCurrentToolProviderWorkflowUpdateRequest))]
[JsonSerializable(typeof(ConsoleApiWorkspacesCurrentToolProviderWorkflowUpdateResponse))]
// Config Options (moved from ConfigJsonSerializerContext)
[JsonSerializable(typeof(EndpointOptions))]
[JsonSerializable(typeof(DifyMigrationOptions))]
[JsonSerializable(typeof(SionWebAppOptions))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
