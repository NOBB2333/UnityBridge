using UnityBridge.Core;

namespace UnityBridge.Api.Dify;

/// <summary>
/// 表示 Dify API 请求的基类。
/// </summary>
public abstract class DifyApiRequest : CommonRequestBase, ICommonRequest
{
    /// <summary>
    /// 获取或设置 Dify API Key (Access Token)。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string? AccessToken { get; set; }
}