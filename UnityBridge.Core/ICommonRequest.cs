namespace UnityBridge.Core;

/// <summary>
/// SKIT.FlurlHttpClient 请求接口。
/// </summary>
public interface ICommonRequest
{
    /// <summary>
    /// 获取或设置请求超时时间（单位：毫秒）。
    /// </summary>
    int? Timeout { get; set; }
}