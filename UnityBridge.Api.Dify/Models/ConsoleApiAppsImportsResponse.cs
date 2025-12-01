namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/apps/imports 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsImportsResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置导入任务 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 获取或设置导入任务状态。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public override string? Status { get; set; }

    /// <summary>
    /// 获取或设置应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// 获取或设置应用模式。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("app_mode")]
    public string? AppMode { get; set; }

    /// <summary>
    /// 获取或设置当前 DSL 版本。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("current_dsl_version")]
    public string? CurrentDslVersion { get; set; }

    /// <summary>
    /// 获取或设置导入的 DSL 版本。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("imported_dsl_version")]
    public string? ImportedDslVersion { get; set; }

    /// <summary>
    /// 获取或设置错误信息。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; set; }
}