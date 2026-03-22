using Flurl.Http;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;
using UnityBridge.Shared;
using AppJsonSerializerContext = UnityBridge.Configuration.Options.AppJsonSerializerContext;

namespace UnityBridge.Platforms.Dify;

/// <summary>
/// 合并了“发布 API Key / 批量发布”和“获取应用信息”的互动命令。
/// </summary>
public static class DifyAppKeyCommand
{
    private const string ExportDir = "exports";

    #region API Key 管理

    public static async Task ManageApiKeysAsync(DifyApiClient client)
    {
        while (true)
        {
            Console.WriteLine("\n请选择操作:");
            Console.WriteLine("1) 从应用列表选择并生成 API Key");
            Console.WriteLine("2) 为所有应用生成 API Key 并导出 CSV");
            Console.WriteLine("3) 查看指定应用的所有 API Key");
            Console.WriteLine("4) 查看所有应用的 API Key 汇总");
            Console.WriteLine("5) 清理冗余 API Key (每个应用只保留1个)");
            Console.WriteLine("0) 返回主菜单");
            Console.Write("输入选项编号后回车: ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    var selectedId = await SelectAppFromListAsync(client);
                    if (selectedId != null)
                    {
                        await GenerateKeyForAppAsync(client, selectedId);
                    }
                    break;
                case "2":
                    await GenerateKeysForAllAppsAsync(client);
                    break;
                case "3":
                    var appId = await SelectAppFromListAsync(client);
                    if (appId != null)
                    {
                        await ListApiKeysForAppAsync(client, appId);
                    }
                    break;
                case "4":
                    await ListAllAppsApiKeysAsync(client);
                    break;
                case "5":
                    await CleanupRedundantApiKeysAsync(client);
                    break;
                case "0":
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

    /// <summary>
    /// 查看指定应用的所有 API Key。
    /// </summary>
    private static async Task ListApiKeysForAppAsync(DifyApiClient client, string appId)
    {
        try
        {
            var listRequest = new ConsoleApiAppsAppidApikeysRequest { AppId = appId };
            var listResponse = await client.ExecuteConsoleApiAppsAppidApikeysAsync(listRequest);

            if (listResponse.Data is not { Length: > 0 })
            {
                Console.WriteLine("\n该应用下暂无 API Key 记录。\n");
                return;
            }

            Console.WriteLine($"\n该应用下共有 {listResponse.Data.Length} 个 API Key：");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            for (int i = 0; i < listResponse.Data.Length; i++)
            {
                var key = listResponse.Data[i];
                var createdText = key.CreatedAt.HasValue 
                    ? DateTimeOffset.FromUnixTimeSeconds(key.CreatedAt.Value).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                    : "N/A";
                var lastUsedText = key.LastUsedAt.HasValue 
                    ? DateTimeOffset.FromUnixTimeSeconds(key.LastUsedAt.Value).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                    : "从未使用";
                
                Console.WriteLine($"[{i + 1}] ID:        {key.Id}");
                Console.WriteLine($"    Token:     {key.Token}");
                Console.WriteLine($"    Created:   {createdText}");
                Console.WriteLine($"    Last Used: {lastUsedText}");
                Console.WriteLine("────────────────────────────────────────────────────────────────────────────────────");
            }
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"获取 API Key 列表失败: {ex.Message}");
            await LogDetailFetchErrorAsync(ex);
        }
    }

    /// <summary>
    /// 查看所有应用的 API Key 汇总。
    /// </summary>
    private static async Task ListAllAppsApiKeysAsync(DifyApiClient client)
    {
        Console.WriteLine("\n正在获取所有应用的 API Key 汇总...");

        var apps = await FetchAllAppsSummaryAsync(client);
        if (apps.Count == 0)
        {
            Console.WriteLine("未获取到任何应用。");
            return;
        }


        Console.WriteLine($"正在获取 {apps.Count} 个应用的 API Key 信息...");

        // 先收集所有数据
        var rows = new List<(string Name, string AppId, int KeyCount, List<string> Tokens, string? Error)>();
        var totalKeys = 0;

        foreach (var app in apps)
        {
            try
            {
                var listRequest = new ConsoleApiAppsAppidApikeysRequest { AppId = app.Id };
                var listResponse = await client.ExecuteConsoleApiAppsAppidApikeysAsync(listRequest);

                var keyCount = listResponse.Data?.Length ?? 0;
                totalKeys += keyCount;

                // 收集所有 Token
                var tokens = keyCount > 0 
                    ? listResponse.Data!.OrderByDescending(k => k.LastUsedAt ?? 0).ThenByDescending(k => k.CreatedAt ?? 0)
                        .Select(k => k.Token ?? "").ToList()
                    : new List<string> { "无" };

                rows.Add((app.Name ?? "", app.Id, keyCount, tokens, null));
            }
            catch (Exception ex)
            {
                rows.Add((app.Name ?? "", app.Id, 0, new List<string>(), ex.Message));
            }

            await Task.Delay(100);
        }

        // 统一打印表格
        Console.WriteLine("\n┏━━━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━━┳━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓");
        Console.WriteLine("┃ 应用名称              ┃ App ID                                 ┃ Key数量 ┃ Token                                             ┃");
        Console.WriteLine("┣━━━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫");

        foreach (var row in rows)
        {
            var name = TruncateOrPad(row.Name, 20);
            var appId = TruncateOrPad(row.AppId, 38);
            var count = TruncateOrPad(row.KeyCount.ToString(), 7);

            if (row.Error != null)
            {
                Console.WriteLine($"┃ {name} ┃ {appId} ┃ 错误    ┃ {TruncateOrPad(row.Error, 49)} ┃");
            }
            else if (row.Tokens.Count == 0)
            {
                Console.WriteLine($"┃ {name} ┃ {appId} ┃ {count} ┃ {TruncateOrPad("无", 49)} ┃");
            }
            else
            {
                // 第一行显示应用信息和第一个 Token
                Console.WriteLine($"┃ {name} ┃ {appId} ┃ {count} ┃ {TruncateOrPad(row.Tokens[0], 49)} ┃");
                
                // 后续行只显示 Token
                for (int i = 1; i < row.Tokens.Count; i++)
                {
                    var emptyName = TruncateOrPad("", 20);
                    var emptyId = TruncateOrPad("", 38);
                    var emptyCount = TruncateOrPad("", 7);
                    Console.WriteLine($"┃ {emptyName} ┃ {emptyId} ┃ {emptyCount} ┃ {TruncateOrPad(row.Tokens[i], 49)} ┃");
                }
            }
        }

        Console.WriteLine("┗━━━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━━┻━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛");
        Console.WriteLine($"\n共 {apps.Count} 个应用，{totalKeys} 个 API Key\n");
    }

    /// <summary>
    /// 清理冗余 API Key，每个应用只保留1个（优先保留最近使用的，若都未使用则保留第一个）。
    /// </summary>
    private static async Task CleanupRedundantApiKeysAsync(DifyApiClient client)
    {
        Console.WriteLine("\n正在获取所有应用...");

        var apps = await FetchAllAppsSummaryAsync(client);
        if (apps.Count == 0)
        {
            Console.WriteLine("未获取到任何应用。");
            return;
        }

        Console.WriteLine($"共 {apps.Count} 个应用，开始清理冗余 API Key...\n");

        var totalDeleted = 0;
        var totalKept = 0;

        foreach (var app in apps)
        {
            try
            {
                var listRequest = new ConsoleApiAppsAppidApikeysRequest { AppId = app.Id };
                var listResponse = await client.ExecuteConsoleApiAppsAppidApikeysAsync(listRequest);

                if (listResponse.Data is not { Length: > 1 })
                {
                    // 0 或 1 个 Key，不需要清理
                    if (listResponse.Data?.Length == 1)
                        totalKept++;
                    continue;
                }

                var keys = listResponse.Data.ToList();
                Console.WriteLine($"[{app.Name}] 有 {keys.Count} 个 API Key，准备清理...");

                // 选择要保留的 Key：优先选最近使用的，若都未使用则选第一个
                var keyToKeep = keys
                    .OrderByDescending(k => k.LastUsedAt ?? 0)  // 最近使用的排前面
                    .ThenBy(k => k.CreatedAt ?? long.MaxValue)  // 相同时保留最早创建的
                    .First();

                Console.WriteLine($"  保留: {keyToKeep.Token} (LastUsed: {(keyToKeep.LastUsedAt.HasValue ? DateTimeOffset.FromUnixTimeSeconds(keyToKeep.LastUsedAt.Value).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : "从未使用")})");

                // 删除其他 Key
                foreach (var key in keys.Where(k => k.Id != keyToKeep.Id))
                {
                    try
                    {
                        var deleteRequest = new ConsoleApiAppsAppidApikeysKeyidRequest
                        {
                            AppId = app.Id,
                            KeyId = key.Id
                        };
                        var success = await client.ExecuteConsoleApiAppsAppidApikeysKeyidDeleteAsync(deleteRequest);
                        if (success)
                        {
                            Console.WriteLine($"  删除: {key.Token}");
                            totalDeleted++;
                        }
                        else
                        {
                            Console.Error.WriteLine($"  删除失败: {key.Token}");
                        }
                    }
                    catch (Exception deleteEx)
                    {
                        Console.Error.WriteLine($"  删除失败: {key.Token} - {deleteEx.Message}");
                    }

                    await Task.Delay(100);
                }

                totalKept++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{app.Name}] 处理失败: {ex.Message}");
            }

            await Task.Delay(100);
        }

        Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"清理完成！删除了 {totalDeleted} 个冗余 Key，保留了 {totalKept} 个 Key");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
    }

    #endregion

    #region 应用信息

    public static async Task InspectAppsAsync(DifyApiClient client)
    {
        while (true)
        {
            Console.WriteLine("\n请选择操作:");
            Console.WriteLine("1) 从应用列表中选择并查看详情");
            Console.WriteLine("2) 下载全部应用详情并导出综合表");
            Console.WriteLine("3) 返回主菜单");
            Console.Write("输入选项编号后回车: ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    var selectedId = await SelectAppFromListAsync(client);
                    if (selectedId != null)
                    {
                        await GetAppInfoAsync(client, selectedId);
                    }
                    break;
                case "2":
                    await ExportAppMatrixAsync(client);
                    break;
                case "3":
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
            ConsoleApiAppsAppidApikeysResponse? apiKeys = null;
            ConsoleApiWorkspacesCurrentToolProviderWorkflowGetResponse? workflowTool = null;

            try
            {
                // 应用详情
                detail = await client.ExecuteConsoleApiAppsAppidAsync(new ConsoleApiAppsAppidRequest { AppId = summary.Id });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"获取 {summary.Name} ({summary.Id}) 详情失败: {ex.Message}");
                await LogDetailFetchErrorAsync(ex);
            }

            try
            {
                // 获取该应用下所有 API Key
                var keyReq = new ConsoleApiAppsAppidApikeysRequest { AppId = summary.Id };
                apiKeys = await client.ExecuteConsoleApiAppsAppidApikeysAsync(keyReq);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"获取 {summary.Name} ({summary.Id}) 的 API Key 列表失败: {ex.Message}");
                await LogDetailFetchErrorAsync(ex);
            }

            // 如果是工作流应用，获取工作流工具信息
            var appMode = detail?.Mode ?? summary.Mode;
            if (string.Equals(appMode, "workflow", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var workflowToolReq = new ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest 
                    { 
                        WorkflowAppId = summary.Id 
                    };
                    workflowTool = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(workflowToolReq);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"获取 {summary.Name} ({summary.Id}) 的工作流工具信息失败: {ex.Message}");
                    await LogDetailFetchErrorAsync(ex);
                }
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
                ApiKeyIds = FormatApiKeyField(apiKeys, k => k.Id),
                ApiKeyTokens = FormatApiKeyField(apiKeys, k => k.Token),
                ApiKeyCreatedAt = FormatApiKeyTimestamp(apiKeys, k => k.CreatedAt, null),
                ApiKeyLastUsedAt = FormatApiKeyTimestamp(apiKeys, k => k.LastUsedAt, "从未使用"),
                WorkflowAppId = workflowTool?.WorkflowAppId,
                WorkflowToolId = workflowTool?.WorkflowToolId,
                HasDetail = detail is not null
            });
            // 延迟
            await Task.Delay(200);
        }

        CsvExportHelper.ExportAndPrint(rows, ExportDir, "apps_matrix.csv");
    }

    /// <summary>
    /// 为所有应用生成 API Key，并将结果导出为 CSV。
    /// 对于工作流应用，会在生成 API Key 前尝试发布一次工作流，并在最后附加已发布工具的 ID（如存在）。
    /// </summary>
    private static async Task GenerateKeysForAllAppsAsync(DifyApiClient client)
    {
        Console.WriteLine("\n开始为所有应用生成 API Key...");

        var apps = await FetchAllAppsSummaryAsync(client);
        if (apps.Count == 0)
        {
            Console.WriteLine("未获取到任何应用，操作终止。");
            return;
        }

        Console.WriteLine($"共 {apps.Count} 个应用，依次处理（先发布工作流，再生成 API Key）...");

        var rows = new List<AllAppKeyRow>(apps.Count);
        var index = 0;

        foreach (var app in apps)
        {
            index++;
            Console.WriteLine($"\n[{index}/{apps.Count}] 处理应用: {app.Name} ({app.Id}), 模式 = {app.Mode}");

            var isWorkflow = string.Equals(app.Mode, "workflow", StringComparison.OrdinalIgnoreCase);
            bool published = false;
            string? publishError = null;
            string? workflowToolId = null;

            // 1. 如果是工作流应用，先发布工作流，并确保已发布为工具
            if (isWorkflow)
            {
                try
                {
                    // 1.1 发布工作流（相当于前端“发布”按钮）
                    var publishRequest = new ConsoleApiAppsAppidWorkflowsPublishRequest
                    {
                        AppId = app.Id,
                        MarkedName = "",
                        MarkedComment = ""
                    };

                    var publishResponse = await client.ExecuteConsoleApiAppsAppidWorkflowsPublishAsync(publishRequest);
                    if (!publishResponse.IsSuccessful())
                    {
                        publishError = $"{publishResponse.ErrorCode} {publishResponse.ErrorMessage}";
                        Console.Error.WriteLine($"  ✗ 工作流发布失败: {publishError}");
                    }
                    else
                    {
                        published = true;
                        Console.WriteLine("  ✓ 工作流已发布");
                    }

                    // 1.2 先尝试获取是否已有发布为工具
                    var toolGetRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest
                    {
                        WorkflowAppId = app.Id
                    };
                    var toolGetResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(toolGetRequest);

                    if (toolGetResponse.IsSuccessful() && !string.IsNullOrEmpty(toolGetResponse.WorkflowToolId))
                    {
                        workflowToolId = toolGetResponse.WorkflowToolId;
                        Console.WriteLine($"  ✓ 已存在工作流工具，ID: {workflowToolId}");
                    }
                    else
                    {
                        // 1.3 如果还没有工具，则调用“发布为工具”接口创建
                        Console.WriteLine("  - 未检测到已发布的工作流工具，准备创建工具...");

                        var createToolRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateRequest
                        {
                            Name = app.Name,
                            Description = app.Description,
                            Icon = new WorkflowToolIcon
                            {
                                Content = app.Icon,
                                Background = app.IconBackground
                            },
                            Label = app.Name,
                            Parameters = new List<WorkflowToolParameter>(),
                            Labels = new List<string>(),
                            PrivacyPolicy = "",
                            WorkflowAppId = app.Id
                        };

                        var createToolResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowCreateAsync(createToolRequest);
                        if (!createToolResponse.IsSuccessful())
                        {
                            Console.Error.WriteLine($"  ✗ 创建工作流工具失败: {createToolResponse.ErrorCode} {createToolResponse.ErrorMessage}");
                        }
                        else
                        {
                            Console.WriteLine("  ✓ 已创建工作流工具");

                            // 再次获取工具 ID 确认
                            var toolGetResponse2 = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(toolGetRequest);
                            if (toolGetResponse2.IsSuccessful() && !string.IsNullOrEmpty(toolGetResponse2.WorkflowToolId))
                            {
                                workflowToolId = toolGetResponse2.WorkflowToolId;
                                Console.WriteLine($"  ✓ 工作流工具 ID: {workflowToolId}");
                            }
                            else
                            {
                                Console.WriteLine("  - 创建工具后仍未获取到 workflow_tool_id。");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    publishError = ex.Message;
                    Console.Error.WriteLine($"  ✗ 工作流发布或发布为工具异常: {publishError}");
                }
            }

            // 2. 生成 API Key
            string? apiKeyId = null;
            string? apiKeyToken = null;
            long? apiKeyCreatedAt = null;
            string? apiKeyError = null;

            try
            {
                var keyRequest = new ConsoleApiAppsAppidApikeysCreateRequest { AppId = app.Id };
                var keyResponse = await client.ExecuteConsoleApiAppsAppidApikeysCreateAsync(keyRequest);
                if (!keyResponse.IsSuccessful())
                {
                    apiKeyError = $"{keyResponse.ErrorCode} {keyResponse.ErrorMessage}";
                    Console.Error.WriteLine($"  ✗ 生成 API Key 失败: {apiKeyError}");
                }
                else
                {
                    apiKeyId = keyResponse.Id;
                    apiKeyToken = keyResponse.Token;
                    apiKeyCreatedAt = keyResponse.CreatedAt;
                    Console.WriteLine("  ✓ 已生成 API Key");
                }
            }
            catch (Exception ex)
            {
                apiKeyError = ex.Message;
                Console.Error.WriteLine($"  ✗ 生成 API Key 异常: {apiKeyError}");
            }

            rows.Add(new AllAppKeyRow
            {
                AppId = app.Id,
                Name = app.Name,
                Mode = app.Mode,
                PublishedWorkflow = published,
                PublishError = publishError,
                ApiKeyId = apiKeyId,
                ApiKeyToken = apiKeyToken,
                ApiKeyCreatedAt = apiKeyCreatedAt,
                ApiKeyError = apiKeyError,
                WorkflowToolId = workflowToolId
            });

            await Task.Delay(150);
        }

        // 打印汇总表格
        PrintApiKeySummaryTable(rows);

        Console.WriteLine();
        CsvExportHelper.ExportAndPrint(rows, ExportDir, "all_app_apikeys.csv", "条应用 API Key 信息");
    }

    /// <summary>
    /// 打印 API Key 生成结果汇总表格。
    /// </summary>
    private static void PrintApiKeySummaryTable(List<AllAppKeyRow> rows)
    {
        Console.WriteLine("\n┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓");
        Console.WriteLine("┃ API Key 生成汇总                                                                                                                  ┃");
        Console.WriteLine("┣━━━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫");
        Console.WriteLine("┃ 应用名称              ┃ App ID                                 ┃ 模式            ┃ API Key Token                                     ┃");
        Console.WriteLine("┣━━━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫");

        foreach (var row in rows)
        {
            var name = TruncateOrPad(row.Name ?? "", 20);
            var appId = TruncateOrPad(row.AppId, 38);
            var mode = TruncateOrPad(row.Mode ?? "", 15);
            var token = string.IsNullOrEmpty(row.ApiKeyToken) 
                ? TruncateOrPad(row.ApiKeyError ?? "❌ 失败", 49)
                : TruncateOrPad(row.ApiKeyToken, 49);

            Console.WriteLine($"┃ {name} ┃ {appId} ┃ {mode} ┃ {token} ┃");
        }

        Console.WriteLine("┗━━━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛");

        // 统计
        var success = rows.Count(r => !string.IsNullOrEmpty(r.ApiKeyToken));
        var failed = rows.Count - success;
        Console.WriteLine($"\n✓ 成功: {success}  ✗ 失败: {failed}  总计: {rows.Count}");
    }

    /// <summary>
    /// 截断或填充字符串到指定长度。
    /// </summary>
    private static string TruncateOrPad(string value, int length)
    {
        if (string.IsNullOrEmpty(value))
            return new string(' ', length);

        // 计算实际显示宽度（中文字符算2个宽度）
        var displayWidth = 0;
        var sb = new System.Text.StringBuilder();
        foreach (var c in value)
        {
            var charWidth = c > 127 ? 2 : 1;
            if (displayWidth + charWidth > length - 1)
            {
                sb.Append('…');
                displayWidth++;
                break;
            }
            sb.Append(c);
            displayWidth += charWidth;
        }

        // 填充到指定宽度
        while (displayWidth < length)
        {
            sb.Append(' ');
            displayWidth++;
        }

        return sb.ToString();
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

    private static string? FormatApiKeyField(ConsoleApiAppsAppidApikeysResponse? response, Func<ConsoleApiAppsAppidApikeysResponse.Types.ApiKey, string?> selector)
    {
        if (response?.Data is not { Length: > 0 })
            return null;

        var list = new List<string>(response.Data.Length);
        foreach (var item in response.Data)
        {
            var value = selector(item);
            if (!string.IsNullOrWhiteSpace(value))
                list.Add(value);
        }

        return list.Count == 0 ? null : string.Join('\n', list);
    }

    private static string? FormatApiKeyTimestamp(ConsoleApiAppsAppidApikeysResponse? response, Func<ConsoleApiAppsAppidApikeysResponse.Types.ApiKey, long?> selector, string? nullPlaceholder)
    {
        if (response?.Data is not { Length: > 0 })
            return null;

        var list = new List<string>(response.Data.Length);
        foreach (var item in response.Data)
        {
            var ts = selector(item);
            if (ts.HasValue)
            {
                list.Add(ts.Value.ToString());
            }
            else if (nullPlaceholder is not null)
            {
                list.Add(nullPlaceholder);
            }
        }

        return list.Count == 0 ? null : string.Join('\n', list);
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
        public string? ApiKeyIds { get; init; }
        public string? ApiKeyTokens { get; init; }
        public string? ApiKeyCreatedAt { get; init; }
        public string? ApiKeyLastUsedAt { get; init; }
        public string? WorkflowAppId { get; init; }
        public string? WorkflowToolId { get; init; }
        public bool HasDetail { get; init; }
    }

    /// <summary>
    /// 为所有应用生成 API Key 导出 CSV 所用的行模型。
    /// </summary>
    private sealed class AllAppKeyRow
    {
        public string AppId { get; init; } = string.Empty;
        public string? Name { get; init; }
        public string? Mode { get; init; }
        public bool PublishedWorkflow { get; init; }
        public string? PublishError { get; init; }
        public string? ApiKeyId { get; init; }
        public string? ApiKeyToken { get; init; }
        public long? ApiKeyCreatedAt { get; init; }
        public string? ApiKeyError { get; init; }
        public string? WorkflowToolId { get; init; }
    }

    #endregion
}

