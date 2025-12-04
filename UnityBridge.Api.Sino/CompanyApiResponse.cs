using UnityBridge.Core;

namespace UnityBridge.Api.Sino;

/// <summary>
/// 表示 Company API 响应的基类。
/// </summary>
public abstract class CompanyApiResponse : CommonResponseBase, ICommonResponse
{
    /// <summary>
    /// 获取或设置错误码。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public virtual int? ErrorCode { get; set; }

    /// <summary>
    /// 获取或设置错误描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("msg")]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 获取或设置 TraceId。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("traceld")]
    public virtual string? TraceId { get; set; }

    /// <summary>
    /// 获取一个值，该值指示调用 API 是否成功。
    /// </summary>
    /// <returns></returns>
    public override bool IsSuccessful()
    {
        return GetRawStatus() == 200 && (ErrorCode == null || ErrorCode == 0 || ErrorCode == 200);
    }
}