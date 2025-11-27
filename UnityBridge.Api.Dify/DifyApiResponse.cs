using UnityBridge.Core;

namespace UnityBridge.Api.Dify;

/// <summary>
/// 表示 Dify API 响应的基类。
/// </summary>
public abstract class DifyApiResponse : CommonResponseBase, ICommonResponse
{
    /// <summary>
    /// 获取或设置 Dify 错误码。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public virtual string? ErrorCode { get; set; }

    /// <summary>
    /// 获取或设置 Dify 错误描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 获取或设置 Dify 状态。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public virtual string? Status { get; set; }

    /// <summary>
    /// 获取一个值，该值指示调用 Dify API 是否成功。
    /// <para>
    ///（即 HTTP 状态码为 200，且 <see cref="ErrorCode"/> 为空或 "success"）。
    /// </para>
    /// </summary>
    /// <returns></returns>
    public override bool IsSuccessful()
    {
        return GetRawStatus() == 200 && (string.IsNullOrEmpty(ErrorCode) || "success".Equals(ErrorCode, System.StringComparison.OrdinalIgnoreCase));
    }
}