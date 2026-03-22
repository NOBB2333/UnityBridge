namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/workspaces/current/tool-provider/workflow/update 接口的响应。</para>
/// </summary>
public class ConsoleApiWorkspacesCurrentToolProviderWorkflowUpdateResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置结果。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("result")]
    public string? Result { get; set; }
}
