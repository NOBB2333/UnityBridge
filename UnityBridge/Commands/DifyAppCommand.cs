using CsvHelper;
using Flurl.Http;
using System.Globalization;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;

namespace UnityBridge.Commands;

/// <summary>
/// 合并了“发布 API Key / 批量发布”和“获取应用信息”的互动命令。
/// </summary>
public static class DifyAppCommand
{
    private const string ExportDir = "exports";

    #region API Key 管理

    public static async Task ManageApiKeysAsync(DifyApiClient client)
    {
        while (true)
        {
            Console.WriteLine("\n请选择操作:");
            Console.WriteLine("1) 为指定应用生成 API Key");
            Console.WriteLine("2) 列出所有应用并选择生成");
            Console.WriteLine("3) 返回主菜单");
            Console.Write("输入选项编号后回车: ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    var appId = PromptAppId();
                    await GenerateKeyForAppAsync(client, appId);
                    break;
                case "2":
                    var selectedId = await SelectAppFromListAsync(client);
                    if (selectedId != null)
                    {
                        await GenerateKeyForAppAsync(client, selectedId);
                    }
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("无效选项，请重新输入。\n");
                    break;
            }
        }
    }

    private static async Task GenerateKeyForAppAsync(DifyApiClient client, string appId)
    {
        try
        {
            var request = new ConsoleApiAppsAppidApikeysCreateRequest { AppId = appId };
            var response = await client.ExecuteConsoleApiAppsAppidApikeysCreateAsync(request);

            Console.WriteLine("\n✓ API Key 生成成功!");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine($"ID:        {response.Id}");
            if (!string.IsNullOrEmpty(response.Token))
            {
                Console.WriteLine($"Token:     {response.Token}");
            }
            Console.WriteLine($"Created:   {(response.CreatedAt.HasValue ? response.CreatedAt.Value.ToString() : "N/A")}");
            Console.WriteLine($"Last Used: {(response.LastUsedAt.HasValue ? response.LastUsedAt.Value.ToString() : "从未使用")}");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            // 再拉一次当前应用下的全部 API Key 并打印
            var listRequest = new ConsoleApiAppsAppidApikeysRequest { AppId = appId };
            var listResponse = await client.ExecuteConsoleApiAppsAppidApikeysAsync(listRequest);

            if (listResponse.Data is { Length: > 0 })
            {
                Console.WriteLine("当前应用下所有 API Key：");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                foreach (var key in listResponse.Data)
                {
                    var createdText = key.CreatedAt.HasValue ? key.CreatedAt.Value.ToString() : "N/A";
                    var lastUsedText = key.LastUsedAt.HasValue ? key.LastUsedAt.Value.ToString() : "从未使用";
                    Console.WriteLine($"ID:        {key.Id}");
                    Console.WriteLine($"Token:     {key.Token}");
                    Console.WriteLine($"Created:   {createdText}");
                    Console.WriteLine($"Last Used: {lastUsedText}");
                    Console.WriteLine("────────────────────────────────────────");
                }
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
            }
            else
            {
                Console.WriteLine("当前应用下暂无 API Key 记录。\n");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"生成或查询 API Key 失败: {ex.Message}");
            await LogDetailFetchErrorAsync(ex);
        }

        await Task.Delay(300);
    }

    #endregion

    #region 应用信息

    public static async Task InspectAppsAsync(DifyApiClient client)
    {
        while (true)
        {
            Console.WriteLine("\n请选择操作:");
            Console.WriteLine("1) 通过应用 ID 获取应用详情");
            Console.WriteLine("2) 从应用列表中选择并查看详情");
            Console.WriteLine("3) 下载全部应用详情并导出综合表");
            Console.WriteLine("4) 返回主菜单");
            Console.Write("输入选项编号后回车: ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    var appId = PromptAppId();
                    await GetAppInfoAsync(client, appId);
                    break;
                case "2":
                    var selectedId = await SelectAppFromListAsync(client);
                    if (selectedId != null)
                    {
                        await GetAppInfoAsync(client, selectedId);
                    }
                    break;
                case "3":
                    await ExportAppMatrixAsync(client);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("无效选项，请重新输入。\n");
                    break;
            }
        }
    }

    // 获取所有的明细
    private static async Task GetAppInfoAsync(DifyApiClient client, string appId)
    {
        // 获取具体明细
        var request = new ConsoleApiAppsAppidRequest { AppId = appId };
        try
        {
            var response = await client.ExecuteConsoleApiAppsAppidAsync(request);

            Console.WriteLine("\n应用信息:");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(response, typeof(ConsoleApiAppsAppidResponse), AppJsonSerializerContext.Default));
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"获取 {appId} 详情失败: {ex.Message}");
            await LogDetailFetchErrorAsync(ex);
        }
    }

    private static async Task ExportAppMatrixAsync(DifyApiClient client)
    {
        Console.WriteLine("\n开始构建“左联”综合表：");
        Console.WriteLine("  - 第一步：分页拉取应用列表（只含基础字段）");
        Console.WriteLine("  - 第二步：逐个拉取应用详情并合并字段");

        var summaries = await FetchAllAppsSummaryAsync(client);
        if (summaries.Count == 0)
        {
            Console.WriteLine("未获取到应用列表，取消导出。");
            return;
        }

        Console.WriteLine($"共获取到 {summaries.Count} 个应用，开始逐个拉取详情并写入矩阵...");

        var rows = new List<AppMatrixRow>(summaries.Count);
        var index = 0;
        foreach (var summary in summaries)
        {
            index++;
            Console.WriteLine($"[{index}/{summaries.Count}] 获取应用详情: {summary.Name} ({summary.Id})");

            ConsoleApiAppsAppidResponse? detail = null;
            try
            {
                detail = await client.ExecuteConsoleApiAppsAppidAsync(new ConsoleApiAppsAppidRequest { AppId = summary.Id });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"获取 {summary.Name} ({summary.Id}) 详情失败: {ex.Message}");
                await LogDetailFetchErrorAsync(ex);
            }

            rows.Add(new AppMatrixRow
            {
                AppId = summary.Id,
                Name = detail?.Name ?? summary.Name,
                Mode = detail?.Mode ?? summary.Mode,
                Description = detail?.Description ?? summary.Description,
                IconType = detail?.IconType ?? summary.IconType,
                Icon = detail?.Icon ?? summary.Icon,
                IconBackground = detail?.IconBackground ?? summary.IconBackground,
                IconUrl = detail?.IconUrl ?? summary.IconUrl,
                Tags = MergeTags(detail, summary),
                MaxActiveRequests = summary.MaxActiveRequests,
                ModelConfig = ResolveModelConfig(detail, summary),
                WorkflowId = detail?.Workflow?.Id ?? summary.Workflow?.Id,
                WorkflowCreatedBy = detail?.Workflow?.CreatedBy ?? summary.Workflow?.CreatedBy,
                WorkflowCreatedAt = detail?.Workflow?.CreatedAt ?? summary.Workflow?.CreatedAt,
                WorkflowUpdatedBy = detail?.Workflow?.UpdatedBy ?? summary.Workflow?.UpdatedBy,
                WorkflowUpdatedAt = detail?.Workflow?.UpdatedAt ?? summary.Workflow?.UpdatedAt,
                UseIconAsAnswerIcon = detail?.UseIconAsAnswerIcon ?? summary.UseIconAsAnswerIcon,
                CreatedBy = detail?.CreatedBy ?? summary.CreatedBy,
                CreatedAt = detail?.CreatedAt ?? summary.CreatedAt,
                UpdatedBy = detail?.UpdatedBy ?? summary.UpdatedBy,
                UpdatedAt = detail?.UpdatedAt ?? summary.UpdatedAt,
                AccessMode = detail?.AccessMode ?? summary.AccessMode,
                CreateUserName = summary.CreateUserName,
                AuthorName = summary.AuthorName,
                EnableSite = detail?.EnableSite,
                EnableApi = detail?.EnableApi,
                SiteAccessToken = detail?.Site?.AccessToken,
                SiteCode = detail?.Site?.Code,
                SiteTitle = detail?.Site?.Title,
                SiteIconType = detail?.Site?.IconType,
                SiteIcon = detail?.Site?.Icon,
                SiteIconBackground = detail?.Site?.IconBackground,
                SiteIconUrl = detail?.Site?.IconUrl,
                SiteDescription = detail?.Site?.Description,
                SiteDefaultLanguage = detail?.Site?.DefaultLanguage,
                SiteChatColorTheme = detail?.Site?.ChatColorTheme,
                SiteChatColorThemeInverted = detail?.Site?.ChatColorThemeInverted,
                SiteCustomizeDomain = detail?.Site?.CustomizeDomain,
                SiteCopyright = detail?.Site?.Copyright,
                SitePrivacyPolicy = detail?.Site?.PrivacyPolicy,
                SiteCustomDisclaimer = detail?.Site?.CustomDisclaimer,
                SiteCustomizeTokenStrategy = detail?.Site?.CustomizeTokenStrategy,
                SitePromptPublic = detail?.Site?.PromptPublic,
                SiteAppBaseUrl = detail?.Site?.AppBaseUrl,
                SiteShowWorkflowSteps = detail?.Site?.ShowWorkflowSteps,
                SiteUseIconAsAnswerIcon = detail?.Site?.UseIconAsAnswerIcon,
                SiteCreatedBy = detail?.Site?.CreatedBy,
                SiteCreatedAt = detail?.Site?.CreatedAt,
                SiteUpdatedBy = detail?.Site?.UpdatedBy,
                SiteUpdatedAt = detail?.Site?.UpdatedAt,
                ApiBaseUrl = detail?.ApiBaseUrl,
                DeletedTools = FormatStringArray(detail?.DeletedTools),
                HasDetail = detail is not null
            });
            await Task.Delay(150);
        }

        if (!Directory.Exists(ExportDir))
        {
            Directory.CreateDirectory(ExportDir);
        }

        var csvPath = Path.Combine(ExportDir, "apps_matrix.csv");
        using (var writer = new StreamWriter(csvPath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(rows);
        }

        Console.WriteLine($"已将 {rows.Count} 条记录导出到 {csvPath}");
    }


    // 获取首页的加载左右的智能体 包括 workflow 和 其他应用
    private static async Task<List<ConsoleApiAppsResponse.Types.App>> FetchAllAppsSummaryAsync(DifyApiClient client)
    {
        const int pageSize = 30;
        var request = new ConsoleApiAppsRequest
        {
            Page = 1,
            Limit = pageSize,
            Name = string.Empty
        };

        var result = new List<ConsoleApiAppsResponse.Types.App>();
        var totalKnown = false;
        var totalCount = 0;
        while (true)
        {
            var pageResponse = await client.ExecuteConsoleApiAppsAsync(request);
            if (pageResponse.Data != null && pageResponse.Data.Length > 0)
            {
                result.AddRange(pageResponse.Data);

                if (!totalKnown && pageResponse.Total > 0)
                {
                    totalKnown = true;
                    totalCount = pageResponse.Total;
                }

                Console.WriteLine(totalKnown
                    ? $"已获取应用列表第 {request.Page} 页，累计 {result.Count}/{totalCount} 条..."
                    : $"已获取应用列表第 {request.Page} 页，累计 {result.Count} 条...");
            }

            if (!pageResponse.HasMore || pageResponse.Data == null || pageResponse.Data.Length == 0)
            {
                break;
            }

            request.Page++;
        }

        return result;
    }

    private static string? MergeTags(ConsoleApiAppsAppidResponse? detail, ConsoleApiAppsResponse.Types.App summary)
    {
        return FormatTags(detail?.Tags) ?? FormatTags(summary.Tags);
    }

    private static string? FormatTags(System.Text.Json.JsonElement[]? tags)
    {
        if (tags is not { Length: > 0 })
            return null;

        var list = new List<string>(tags.Length);
        foreach (var tag in tags)
        {
            switch (tag.ValueKind)
            {
                case System.Text.Json.JsonValueKind.String:
                    {
                        var value = tag.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            list.Add(value);
                        break;
                    }

                case System.Text.Json.JsonValueKind.Object:
                    {
                        if (tag.TryGetProperty("name", out var nameElement))
                        {
                            var nameValue = nameElement.GetString();
                            if (!string.IsNullOrWhiteSpace(nameValue))
                            {
                                list.Add(nameValue);
                                break;
                            }
                        }

                        var raw = tag.GetRawText();
                        if (!string.IsNullOrWhiteSpace(raw))
                            list.Add(raw);
                        break;
                    }

                default:
                    {
                        var raw = tag.GetRawText();
                        if (!string.IsNullOrWhiteSpace(raw))
                            list.Add(raw);
                        break;
                    }
            }
        }

        return list.Count == 0 ? null : string.Join('|', list);
    }

    private static string? FormatStringArray(string[]? values)
    {
        if (values is not { Length: > 0 })
            return null;
        var list = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
        return list.Length == 0 ? null : string.Join('|', list);
    }

    private static string? ResolveModelConfig(ConsoleApiAppsAppidResponse? detail, ConsoleApiAppsResponse.Types.App summary)
    {
        if (detail?.ModelConfig is System.Text.Json.JsonElement detailElement)
            return detailElement.GetRawText();

        return summary.ModelConfig.HasValue ? summary.ModelConfig.Value.GetRawText() : null;
    }

    private static async Task LogDetailFetchErrorAsync(Exception ex)
    {
        switch (ex)
        {
            case FlurlParsingException parsingEx:
                await LogFlurlExceptionAsync(parsingEx);
                break;
            case FlurlHttpException httpEx:
                await LogFlurlExceptionAsync(httpEx);
                break;
            default:
                Console.Error.WriteLine(ex);
                break;
        }
    }

    private static async Task LogFlurlExceptionAsync(FlurlHttpException ex)
    {
        Console.Error.WriteLine($"Flurl Exception: {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException is not null)
        {
            Console.Error.WriteLine($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }

        var status = ex.Call?.Response?.StatusCode;
        if (status.HasValue)
        {
            Console.Error.WriteLine($"HTTP Status: {status.Value}");
        }

        if (ex.Call?.Request != null)
        {
            Console.Error.WriteLine($"Request: {ex.Call.Request.Verb} {ex.Call.Request.Url}");
        }

        if (ex.Call?.Response is not null)
        {
            string body;
            try
            {
                body = await ex.Call.Response.GetStringAsync();
            }
            catch (Exception bodyEx)
            {
                body = $"<读取响应体失败: {bodyEx.Message}>";
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                Console.Error.WriteLine($"Response Body: {body}");
            }
        }
    }

    #endregion

    #region 通用交互辅助

    private static string PromptAppId()
    {
        while (true)
        {
            Console.Write("请输入应用 ID: ");
            var appId = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(appId))
            {
                return appId;
            }
            Console.WriteLine("应用 ID 不能为空，请重新输入。\n");
        }
    }

    private static async Task<string?> SelectAppFromListAsync(DifyApiClient client)
    {
        var listRequest = new ConsoleApiAppsRequest { Page = 1, Limit = 30 };
        var listResponse = await client.ExecuteConsoleApiAppsAsync(listRequest);

        if (listResponse.Data == null || listResponse.Data.Length == 0)
        {
            Console.WriteLine("没有找到任何应用。");
            return null;
        }

        Console.WriteLine("\n应用列表:");
        for (int i = 0; i < listResponse.Data.Length; i++)
        {
            Console.WriteLine($"  {i + 1}: {listResponse.Data[i].Name} ({listResponse.Data[i].Id})");
        }

        while (true)
        {
            Console.Write("\n请选择应用编号 (输入 0 取消): ");
            var choice = Console.ReadLine()?.Trim();

            if (choice == "0")
                return null;

            if (int.TryParse(choice, out int num) && num > 0 && num <= listResponse.Data.Length)
            {
                return listResponse.Data[num - 1].Id;
            }
            Console.WriteLine("无效选项，请重新输入。");
        }
    }

    private sealed class AppMatrixRow
    {
        public string AppId { get; init; } = string.Empty;
        public string? Name { get; init; }
        public string? Mode { get; init; }
        public string? Description { get; init; }
        public string? IconType { get; init; }
        public string? Icon { get; init; }
        public string? IconBackground { get; init; }
        public string? IconUrl { get; init; }
        public string? Tags { get; init; }
        public int? MaxActiveRequests { get; init; }
        public string? ModelConfig { get; init; }
        public string? WorkflowId { get; init; }
        public string? WorkflowCreatedBy { get; init; }
        public long? WorkflowCreatedAt { get; init; }
        public string? WorkflowUpdatedBy { get; init; }
        public long? WorkflowUpdatedAt { get; init; }
        public bool? UseIconAsAnswerIcon { get; init; }
        public string? CreatedBy { get; init; }
        public long? CreatedAt { get; init; }
        public string? UpdatedBy { get; init; }
        public long? UpdatedAt { get; init; }
        public string? AccessMode { get; init; }
        public string? CreateUserName { get; init; }
        public string? AuthorName { get; init; }
        public bool? EnableSite { get; init; }
        public bool? EnableApi { get; init; }
        public string? SiteAccessToken { get; init; }
        public string? SiteCode { get; init; }
        public string? SiteTitle { get; init; }
        public string? SiteIconType { get; init; }
        public string? SiteIcon { get; init; }
        public string? SiteIconBackground { get; init; }
        public string? SiteIconUrl { get; init; }
        public string? SiteDescription { get; init; }
        public string? SiteDefaultLanguage { get; init; }
        public string? SiteChatColorTheme { get; init; }
        public bool? SiteChatColorThemeInverted { get; init; }
        public string? SiteCustomizeDomain { get; init; }
        public string? SiteCopyright { get; init; }
        public string? SitePrivacyPolicy { get; init; }
        public string? SiteCustomDisclaimer { get; init; }
        public string? SiteCustomizeTokenStrategy { get; init; }
        public bool? SitePromptPublic { get; init; }
        public string? SiteAppBaseUrl { get; init; }
        public bool? SiteShowWorkflowSteps { get; init; }
        public bool? SiteUseIconAsAnswerIcon { get; init; }
        public string? SiteCreatedBy { get; init; }
        public long? SiteCreatedAt { get; init; }
        public string? SiteUpdatedBy { get; init; }
        public long? SiteUpdatedAt { get; init; }
        public string? ApiBaseUrl { get; init; }
        public string? DeletedTools { get; init; }
        public bool HasDetail { get; init; }
    }

    #endregion
}

