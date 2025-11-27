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
    /// 获取或设置默认 Access Token。
    /// </summary>
    public string AccessToken { get; set; } = default!;
}