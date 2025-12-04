using UnityBridge.Core;

namespace UnityBridge.Api.Sino;

/// <summary>
/// 表示 Company API 请求的基类。
/// </summary>
public abstract class CompanyApiRequest : CommonRequestBase, ICommonRequest
{
    /// <summary>
    /// 获取或设置 Token（对应 HTTP 请求头中的 <c>Token</c>）。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? Token { get; set; }

    /// <summary>
    /// 获取或设置 Over Token（对应 HTTP 请求头中的 <c>OVERTOKEN</c>）。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? OverToken { get; set; }

    /// <summary>
    /// 获取或设置 TenantId。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? TenantId { get; set; }

    /// <summary>
    /// 获取或设置 Origin。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? Origin { get; set; }

    /// <summary>
    /// 获取或设置 Referer。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? Referer { get; set; }

    /// <summary>
    /// 获取或设置 Accept-Language。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? AcceptLanguage { get; set; }

    /// <summary>
    /// 获取或设置 User-Agent。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// 获取或设置额外的 HTTP 请求头。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual IDictionary<string, string>? ExtraHeaders { get; set; }
}