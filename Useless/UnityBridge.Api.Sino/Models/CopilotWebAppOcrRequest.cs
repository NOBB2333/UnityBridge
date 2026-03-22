namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /copilot-web-app/ocr 接口的请求。</para>
/// </summary>
public class CopilotWebAppOcrRequest : CompanyApiRequest
{
    /// <summary>
    /// 获取或设置文件路径。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? FileName { get; set; }

    /// <summary>
    /// 获取或设置文件内容类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ContentType { get; set; }

    /// <summary>
    /// 获取或设置文件字节数组。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public byte[]? FileBytes { get; set; }
}