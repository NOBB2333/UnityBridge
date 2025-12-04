using UnityBridge.Core;

namespace UnityBridge.Api.Sino;

/// <summary>
/// 一个用于构造 <see cref="CompanyApiClient"/> 时使用的配置项。
/// </summary>
public class CompanyApiClientOptions : CommonClientOptions
{
    /// <summary>
    /// 获取或设置 API 接口域名。
    /// <para>
    /// 默认值：<see cref="CompanyApiEndpoints.DEFAULT"/>
    /// </para>
    /// </summary>
    public string Endpoint { get; set; } = CompanyApiEndpoints.DEFAULT;

    /// <summary>
    /// 获取或设置默认 Token（对应 HTTP 请求头中的 <c>Token</c>）。
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 获取或设置默认 Over Token（对应 HTTP 请求头中的 <c>OVERTOKEN</c>）。
    /// </summary>
    public string? OverToken { get; set; }

    /// <summary>
    /// 获取或设置默认 TenantId（对应 HTTP 请求头中的 <c>TenantId</c>）。
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// 获取或设置默认 Origin。
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// 获取或设置默认 Referer。
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// 获取或设置默认 Accept-Language。
    /// </summary>
    public string? AcceptLanguage { get; set; } = "zh-CN,zh;q=0.9";

    /// <summary>
    /// 获取或设置默认 User-Agent。
    /// </summary>
    public string? UserAgent { get; set; } = "UnityBridge.Api.Sino/1.0 (+https://github.com/your-org/unitybridge)";
}