using System.Text.Json;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;

namespace UnityBridge.Platforms.Dify;

/// <summary>
/// 工作流发布和工具创建服务。
/// </summary>
public static class DifyWorkflowPublishCommand
{

    /// <summary>
    /// 交互式工作流管理菜单。
    /// </summary>
    public static async Task ManageWorkflowsAsync(DifyApiClient client)
    {
        while (true)
        {
            Console.WriteLine("\n请选择操作:");
            Console.WriteLine("1) 发布所有流程 (workflow + advanced-chat)");
            Console.WriteLine("2) 只发布 Chatflow (advanced-chat)");
            Console.WriteLine("3) 只发布 Workflow (workflow)");
            Console.WriteLine("4) 发布工作流并创建工具");
            Console.WriteLine("0) 返回主菜单");
            Console.Write("输入选项编号后回车: ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    await PublishByModeAsync(client, null); // 所有模式
                    break;
                case "2":
                    await PublishByModeAsync(client, "advanced-chat");
                    break;
                case "3":
                    await PublishByModeAsync(client, "workflow");
                    break;
                case "4":
                    await PublishAllWorkflowsAndToolsAsync(client);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("无效选项，请重新输入。\n");
                    break;
            }
        }
    }


    /// <summary>
    /// 发布单个工作流并创建工具（核心逻辑，供其他方法复用）。
    /// </summary>
    /// <param name="client">Dify API 客户端</param>
    /// <param name="appId">应用 ID</param>
    /// <param name="appName">应用名称（用于日志和工具名）</param>
    /// <param name="skipIfToolExists">如果工具已存在是否跳过</param>
    /// <returns>工具 ID，如果失败则返回 null</returns>
    public static async Task<string?> PublishSingleWorkflowAsync(
        DifyApiClient client,
        string appId,
        string appName,
        bool skipIfToolExists = true)
    {
        try
        {
            Console.WriteLine($"处理工作流: {appName} (appId: {appId})");

            // 1. 发布工作流
            var publishRequest = new ConsoleApiAppsAppidWorkflowsPublishRequest
            {
                AppId = appId,
                MarkedName = "",
                MarkedComment = ""
            };

            var publishResponse = await client.ExecuteConsoleApiAppsAppidWorkflowsPublishAsync(publishRequest);
            if (!publishResponse.IsSuccessful())
            {
                Console.Error.WriteLine($"  ✗ 发布失败: {publishResponse.ErrorCode} {publishResponse.ErrorMessage}");
                return null;
            }

            Console.WriteLine($"  ✓ 工作流已发布");

            // 2. 检查工具是否已存在（容忍解析失败）
            string? existingToolId = null;
            try
            {
                var getToolRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest { WorkflowAppId = appId };
                var getToolResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(getToolRequest);

                if (getToolResponse.IsSuccessful() && !string.IsNullOrEmpty(getToolResponse.WorkflowToolId))
                {
                    existingToolId = getToolResponse.WorkflowToolId;
                    Console.WriteLine($"  - 检测到现有工具 (ID: {existingToolId})");
                }
            }
            catch
            {
                // GET 请求失败（可能工具不存在），继续创建
                Console.WriteLine($"  - 未找到现有工具，准备创建...");
            }

            // 3. 根据工具是否存在，决定调用 create 还是 update
            if (!string.IsNullOrEmpty(existingToolId))
            {
                // 工具已存在，调用 update
                Console.WriteLine($"  - 更新工具 (ID: {existingToolId})...");
                var updateToolRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowUpdateRequest
                {
                    Name = SanitizeToolName(appName),
                    Description = appName,
                    Icon = new WorkflowToolIcon { Content = "🔧", Background = "#4A90D9" },
                    Label = appName,
                    Parameters = new List<WorkflowToolParameter>(),
                    Labels = new List<string>(),
                    PrivacyPolicy = "",
                    WorkflowToolId = existingToolId
                };

                var updateToolResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowUpdateAsync(updateToolRequest);
                if (!updateToolResponse.IsSuccessful())
                {
                    Console.Error.WriteLine($"  ✗ 更新工具失败: {updateToolResponse.ErrorCode} {updateToolResponse.ErrorMessage}");
                    return null;
                }

                Console.WriteLine($"  ✓ 工具已更新");
                return existingToolId;
            }
            else
            {
                // 工具不存在，调用 create
                Console.WriteLine($"  - 创建工具...");
                var createToolRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateRequest
                {
                    Name = SanitizeToolName(appName),
                    Description = appName,
                    Icon = new WorkflowToolIcon { Content = "🔧", Background = "#4A90D9" },
                    Label = appName,
                    Parameters = new List<WorkflowToolParameter>(),
                    Labels = new List<string>(),
                    PrivacyPolicy = "",
                    WorkflowAppId = appId
                };

                var createToolResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowCreateAsync(createToolRequest);
                if (!createToolResponse.IsSuccessful())
                {
                    Console.Error.WriteLine($"  ✗ 创建工具失败: {createToolResponse.ErrorCode} {createToolResponse.ErrorMessage}");
                    return null;
                }

                Console.WriteLine($"  ✓ 工具已创建");

                // 4. 尝试获取工具 ID 确认（容忍失败）
                try
                {
                    var getToolRequest2 = new ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest { WorkflowAppId = appId };
                    var getToolResponse2 = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(getToolRequest2);
                    if (getToolResponse2.IsSuccessful() && !string.IsNullOrEmpty(getToolResponse2.WorkflowToolId))
                    {
                        Console.WriteLine($"  ✓ 工具 ID: {getToolResponse2.WorkflowToolId}");
                        return getToolResponse2.WorkflowToolId;
                    }
                }
                catch
                {
                    // 获取工具 ID 失败，但工具已创建成功
                }

                // 工具创建成功但无法获取 ID
                return "created";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  ✗ 处理失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 将应用名称转换为合法的工具名（只保留字母数字和下划线）。
    /// </summary>
    private static string SanitizeToolName(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
            else if (c == ' ' || c == '-')
                sb.Append('_');
        }
        var result = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(result) ? "workflow_tool" : result;
    }

    /// <summary>
    /// 按模式发布工作流（只点发布按钮，不创建工具）。
    /// </summary>
    /// <param name="client">Dify API 客户端</param>
    /// <param name="mode">模式过滤：null=全部，"workflow"=只发布workflow，"advanced-chat"=只发布chatflow</param>
    public static async Task PublishByModeAsync(DifyApiClient client, string? mode)
    {
        var modeName = mode switch
        {
            "workflow" => "Workflow",
            "advanced-chat" => "Chatflow (advanced-chat)",
            _ => "所有流程"
        };

        Console.WriteLine($"\n开始获取应用列表，准备发布 {modeName}...");

        var allApps = await DifyApiHelper.FetchAllAppsAsync(client, 30, (page, count) =>
        {
            Console.WriteLine($"已获取应用列表第 {page} 页，累计 {count} 条...");
        });

        // 过滤出需要发布的应用
        var targetApps = mode == null 
            ? allApps.Where(app => app.Mode is "workflow" or "advanced-chat").ToList()
            : allApps.Where(app => string.Equals(app.Mode, mode, StringComparison.OrdinalIgnoreCase)).ToList();

        if (targetApps.Count == 0)
        {
            Console.WriteLine($"未找到任何 {modeName} 应用。");
            return;
        }

        Console.WriteLine($"找到 {targetApps.Count} 个 {modeName} 应用，开始批量发布...\n");

        var successCount = 0;
        var failCount = 0;

        foreach (var app in targetApps)
        {
            var success = await PublishWorkflowOnlyAsync(client, app.Id, app.Name ?? "Unknown");
            if (success)
                successCount++;
            else
                failCount++;

            await Task.Delay(200);
        }

        Console.WriteLine($"\n批量发布完成！成功: {successCount}，失败: {failCount}\n");
    }

    /// <summary>
    /// 只发布单个工作流（不创建工具）。
    /// </summary>
    public static async Task<bool> PublishWorkflowOnlyAsync(DifyApiClient client, string appId, string appName)
    {
        try
        {
            Console.WriteLine($"发布: {appName} ({appId})");

            var publishRequest = new ConsoleApiAppsAppidWorkflowsPublishRequest
            {
                AppId = appId,
                MarkedName = "",
                MarkedComment = ""
            };

            var publishResponse = await client.ExecuteConsoleApiAppsAppidWorkflowsPublishAsync(publishRequest);
            if (!publishResponse.IsSuccessful())
            {
                Console.Error.WriteLine($"  ✗ 发布失败: {publishResponse.ErrorCode} {publishResponse.ErrorMessage}");
                return false;
            }

            Console.WriteLine($"  ✓ 已发布");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  ✗ 发布失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 批量发布所有工作流应用并创建工具（原逻辑）。
    /// </summary>
    public static async Task PublishAllWorkflowsAndToolsAsync(DifyApiClient client)
    {
        Console.WriteLine("\n开始获取所有应用列表...");

        var allApps = await DifyApiHelper.FetchAllAppsAsync(client, 30, (page, count) =>
        {
            Console.WriteLine($"已获取应用列表第 {page} 页，累计 {count} 条...");
        });

        var workflowApps = allApps
            .Where(app => string.Equals(app.Mode, "workflow", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (workflowApps.Count == 0)
        {
            Console.WriteLine("未找到任何工作流应用。");
            return;
        }

        Console.WriteLine($"找到 {workflowApps.Count} 个工作流应用，开始批量发布并创建工具...\n");

        var successCount = 0;
        var failCount = 0;

        foreach (var app in workflowApps)
        {
            var toolId = await PublishSingleWorkflowAsync(client, app.Id, app.Name ?? "Unknown");
            if (toolId != null)
                successCount++;
            else
                failCount++;

            await Task.Delay(300);
        }

        Console.WriteLine($"\n批量发布完成！成功: {successCount}，失败: {failCount}\n");
    }

    /// <summary>
    /// 从发布数据中提取工具配置信息。
    /// </summary>
    internal static WorkflowToolConfig? ExtractToolConfigFromPublishData(JsonElement data, string fileName)
    {
        try
        {
            if (data.ValueKind == JsonValueKind.Object)
            {
                if (data.TryGetProperty("tool", out var toolElement) && toolElement.ValueKind == JsonValueKind.Object)
                {
                    var name = toolElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    var description = toolElement.TryGetProperty("description", out var descProp)
                        ? (descProp.ValueKind == JsonValueKind.Object
                            ? descProp.TryGetProperty("zh_Hans", out var zhProp) ? zhProp.GetString() : null
                            : descProp.GetString())
                        : null;

                    // 提取 label（优先使用中文）
                    string? label = null;
                    if (toolElement.TryGetProperty("label", out var labelProp))
                    {
                        if (labelProp.ValueKind == JsonValueKind.Object)
                        {
                            label = labelProp.TryGetProperty("zh_Hans", out var zhLabel) ? zhLabel.GetString() :
                                   labelProp.TryGetProperty("en_US", out var enLabel) ? enLabel.GetString() : null;
                        }
                        else
                        {
                            label = labelProp.GetString();
                        }
                    }

                    // 提取参数
                    var parameters = new List<WorkflowToolParameter>();
                    if (toolElement.TryGetProperty("parameters", out var paramsProp) && paramsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var param in paramsProp.EnumerateArray())
                        {
                            var paramName = param.TryGetProperty("name", out var pn) ? pn.GetString() : null;
                            var paramDesc = param.TryGetProperty("llm_description", out var pd) ? pd.GetString() :
                                          param.TryGetProperty("description", out var pd2) ? pd2.GetString() : null;
                            var paramForm = param.TryGetProperty("form", out var pf) ? pf.GetString() : "llm";

                            if (!string.IsNullOrEmpty(paramName))
                            {
                                parameters.Add(new WorkflowToolParameter
                                {
                                    Name = paramName,
                                    Description = paramDesc,
                                    Form = paramForm
                                });
                            }
                        }
                    }

                    // 提取图标
                    WorkflowToolIcon? icon = null;
                    if (data.TryGetProperty("icon", out var iconProp) && iconProp.ValueKind == JsonValueKind.Object)
                    {
                        icon = new WorkflowToolIcon
                        {
                            Content = iconProp.TryGetProperty("content", out var ic) ? ic.GetString() : null,
                            Background = iconProp.TryGetProperty("background", out var ib) ? ib.GetString() : null
                        };
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        return new WorkflowToolConfig
                        {
                            Name = name,
                            Description = description,
                            Label = label,
                            Parameters = parameters,
                            Icon = icon,
                            Labels = new List<string>(),
                            PrivacyPolicy = ""
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error extracting tool config: {ex.Message}");
        }

        return null;
    }
}

/// <summary>
/// 工作流工具配置信息。
/// </summary>
public class WorkflowToolConfig
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Label { get; set; }
    public List<WorkflowToolParameter>? Parameters { get; set; }
    public WorkflowToolIcon? Icon { get; set; }
    public List<string>? Labels { get; set; }
    public string? PrivacyPolicy { get; set; }
}
