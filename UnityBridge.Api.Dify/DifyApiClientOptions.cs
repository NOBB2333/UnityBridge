using UnityBridge.Core;

namespace UnityBridge.Api.Dify;

/// <summary>
/// 一个用于构造 <see cref="DifyApiClient"/> 时使用的配置项。
/// </summary>
public class DifyApiClientOptions : CommonClientOptions
{
    /// <summary>
    /// 获取或设置 API 接口域名。
    /// <para>
    /// 默认值：<see cref="DifyApiEndpoints.DEFAULT"/>
    /// </para>
    /// </summary>
    public string Endpoint { get; set; } = DifyApiEndpoints.DEFAULT;

    /// <summary>
    /// 获取或设置默认 API Key。
    /// </summary>
    public string AppKey { get; set; } = default!;
}