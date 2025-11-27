namespace UnityBridge.Core;

/// <summary>
/// SKIT.FlurlHttpClient 客户端配置项基类。
/// </summary>
public abstract class CommonClientOptions
{
    /// <summary>
    /// 获取或设置请求超时时间（单位：毫秒）。
    /// </summary>
    public int Timeout { get; set; } = 30 * 1000;
}