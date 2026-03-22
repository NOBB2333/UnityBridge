using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;

namespace UnityBridge.Platforms.Dify;

/// <summary>
/// Dify API 公共调用方法。
/// </summary>
public static class DifyApiHelper
{
    /// <summary>
    /// 分页获取所有应用列表。
    /// </summary>
    public static async Task<List<ConsoleApiAppsResponse.Types.App>> FetchAllAppsAsync(
        DifyApiClient client,
        int pageSize = 30,
        Action<int, int>? onProgress = null)
    {
        var listRequest = new ConsoleApiAppsRequest
        {
            Page = 1,
            Limit = pageSize,
            Name = string.Empty
        };

        var allApps = new List<ConsoleApiAppsResponse.Types.App>();
        while (true)
        {
            var pageResponse = await client.ExecuteConsoleApiAppsAsync(listRequest);
            if (pageResponse.Data is { Length: > 0 })
            {
                allApps.AddRange(pageResponse.Data);
                onProgress?.Invoke(listRequest.Page, allApps.Count);
            }

            if (!pageResponse.HasMore || pageResponse.Data == null || pageResponse.Data.Length == 0)
            {
                break;
            }

            listRequest.Page++;
        }

        return allApps;
    }

    /// <summary>
    /// 获取指定模式的应用（如 workflow, advanced-chat 等）。
    /// </summary>
    public static async Task<List<ConsoleApiAppsResponse.Types.App>> FetchAppsByModeAsync(
        DifyApiClient client,
        string mode,
        int pageSize = 30)
    {
        var allApps = await FetchAllAppsAsync(client, pageSize);
        return allApps
            .Where(app => string.Equals(app.Mode, mode, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
