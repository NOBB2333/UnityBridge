namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps 接口的请求。</para>
/// </summary>
public class ConsoleApiAppsRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置页码。
    /// <para>默认值：1</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int Page { get; set; } = 1;

    /// <summary>
    /// 获取或设置每页数量。
    /// <para>默认值：20</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int Limit { get; set; } = 20;

    /// <summary>
    /// 获取或设置应用名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? Name { get; set; }

    /// <summary>
    /// 获取或设置是否只返回我创建的应用。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool? IsCreatedByMe { get; set; }
}