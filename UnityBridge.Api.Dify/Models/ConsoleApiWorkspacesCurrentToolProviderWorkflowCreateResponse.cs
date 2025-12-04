namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/workspaces/current/tool-provider/workflow/create 接口的响应。</para>
/// </summary>
public class ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置结果。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("result")]
    public string? Result { get; set; }
}

