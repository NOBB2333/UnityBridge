namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps/imports/{import_id}/check-dependencies 接口的请求。</para>
/// </summary>
public class ConsoleApiAppsImportsCheckDependenciesRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置导入任务 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ImportId { get; set; } = string.Empty;
}

