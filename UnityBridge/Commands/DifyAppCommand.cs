using CsvHelper;
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
        var request = new ConsoleApiAppsAppidApikeysCreateRequest { AppId = appId };
        var response = await client.ExecuteConsoleApiAppsAppidApikeysCreateAsync(request);

        Console.WriteLine("\n✓ API Key 生成成功!");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"ID:        {response.Data.Id}");
        if (!string.IsNullOrEmpty(response.Data.Token))
        {
            Console.WriteLine($"Token:     {response.Data.Token}");
        }
        Console.WriteLine($"Created:   {response.Data.CreatedAt ?? "N/A"}");
        Console.WriteLine($"Last Used: {(string.IsNullOrEmpty(response.Data.LastUsedAt) ? "从未使用" : response.Data.LastUsedAt)}");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

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

    private static async Task GetAppInfoAsync(DifyApiClient client, string appId)
    {
        var request = new ConsoleApiAppsAppidRequest { AppId = appId };
        var response = await client.ExecuteConsoleApiAppsAppidAsync(request);

        Console.WriteLine("\n应用信息:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(response, typeof(ConsoleApiAppsAppidResponse), AppJsonSerializerContext.Default));
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
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
            }

            rows.Add(new AppMatrixRow
            {
                AppId = summary.Id,
                Name = summary.Name,
                SummaryMode = summary.Mode,
                DetailMode = detail?.Mode,
                SummaryDescription = summary.Description,
                DetailDescription = detail?.Description,
                IconType = detail?.IconType ?? summary.IconType,
                IconUrl = detail?.IconUrl ?? summary.IconUrl,
                Tags = FormatTags(summary.Tags),
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

    private static string? FormatTags(System.Text.Json.JsonElement[]? tags)
    {
        if (tags is not { Length: > 0 })
            return null;

        var list = new List<string>(tags.Length);
        foreach (var tag in tags)
        {
            if (tag.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = tag.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    list.Add(value);
            }
            else
            {
                list.Add(tag.GetRawText());
            }
        }

        return list.Count == 0 ? null : string.Join('|', list);
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
        var listRequest = new ConsoleApiAppsRequest { Page = 1, Limit = 100 };
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
        public string? SummaryMode { get; init; }
        public string? DetailMode { get; init; }
        public string? SummaryDescription { get; init; }
        public string? DetailDescription { get; init; }
        public string? IconType { get; init; }
        public string? IconUrl { get; init; }
        public string? Tags { get; init; }
        public bool HasDetail { get; init; }
    }

    #endregion
}

