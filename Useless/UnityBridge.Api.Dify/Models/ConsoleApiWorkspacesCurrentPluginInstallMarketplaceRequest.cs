namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/workspaces/current/plugin/install/marketplace 接口的请求。</para>
/// <para>从应用市场安装插件。</para>
/// </summary>
public class ConsoleApiWorkspacesCurrentPluginInstallMarketplaceRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置插件唯一标识符列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("plugin_unique_identifiers")]
    public List<string> PluginUniqueIdentifiers { get; set; } = new();
}

