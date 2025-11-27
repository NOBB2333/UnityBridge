namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/apps/imports 接口的请求。</para>
/// </summary>
public class ConsoleApiAppsImportsRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置导入模式。
    /// <para>可选值：yaml-content, yaml-file</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("mode")]
    public string Mode { get; set; } = "yaml-content";

    /// <summary>
    /// 获取或设置 YAML 内容。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("yaml_content")]
    public string? YamlContent { get; set; }
}