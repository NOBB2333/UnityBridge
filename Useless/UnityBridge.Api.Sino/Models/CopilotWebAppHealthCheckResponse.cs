namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [GET] /copilot-web-app/chat 接口的响应。</para>
/// </summary>
public class CopilotWebAppHealthCheckResponse : CompanyApiResponse
{
    /// <summary>
    /// 获取或设置 HTML 内容。
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// 获取或设置原始 HTTP 状态码。
    /// </summary>
    public new int RawStatus { get; set; }

    /// <summary>
    /// 获取服务是否健康。
    /// </summary>
    public bool IsHealthy => RawStatus == 200;

    /// <inheritdoc/>
    public override int GetRawStatus()
    {
        return RawStatus;
    }
}
