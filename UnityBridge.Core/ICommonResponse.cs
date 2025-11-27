namespace UnityBridge.Core;

/// <summary>
/// SKIT.FlurlHttpClient 响应接口。
/// </summary>
public interface ICommonResponse
{
    /// <summary>
    /// 获取原始 HTTP 状态码。
    /// </summary>
    int GetRawStatus();

    /// <summary>
    /// 获取原始 HTTP 响应头集合。
    /// </summary>
    IDictionary<string, string> GetRawHeaders();

    /// <summary>
    /// 获取原始 HTTP 响应正文。
    /// </summary>
    byte[] GetRawBytes();

    /// <summary>
    /// 获取一个值，该值指示调用 API 是否成功。
    /// </summary>
    bool IsSuccessful();
}