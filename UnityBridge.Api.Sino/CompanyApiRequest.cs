using UnityBridge.Core;

namespace UnityBridge.Api.Sino;

/// <summary>
/// 表示 Company API 请求的基类。
/// </summary>
public abstract class CompanyApiRequest : CommonRequestBase, ICommonRequest
{
    /// <summary>
    /// 获取或设置 Access Token。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? AccessToken { get; set; }
}