using CsvHelper;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;
using UnityBridge.Helpers;

namespace UnityBridge.Commands;

/// <summary>
/// 包含 Dify 导入导出相关命令。
/// </summary>
public static class DifyMigrationCommand
{
    private const string ExportDir = "exports2";

    /// <summary>
    /// 导出所有应用（原 DownloadCommand.RunAsync）。
    /// </summary>
    public static async Task ExportAsync(DifyApiClient client)
    {
        const int pageSize = 30;
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
            if (pageResponse.Data != null && pageResponse.Data.Length > 0)
            {
                allApps.AddRange(pageResponse.Data);
            }

            if (!pageResponse.HasMore || pageResponse.Data == null || pageResponse.Data.Length == 0)
            {
                break;
            }

            listRequest.Page++;
        }

        if (!Directory.Exists(ExportDir))
            Directory.CreateDirectory(ExportDir);

        Console.WriteLine($"data count: {allApps.Count}");
        var csvPath = Path.Combine(ExportDir, "apps.csv");

        using (var writer = new StreamWriter(csvPath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(allApps);
        }
        Console.WriteLine($"Saved app list CSV: {csvPath}");

        if (allApps.Count > 0)
        {
            foreach (var app in allApps)
            {
                var id = app.Id;
                var name = app.Name;

                var exportRequest = new ConsoleApiAppsAppidExportRequest { AppId = id, IncludeSecret = false };
                var exportResponse = await client.ExecuteConsoleApiAppsAppidExportAsync(exportRequest);

                string yamlContent;
                if (!string.IsNullOrEmpty(exportResponse.RawText))
                {
                    yamlContent = exportResponse.RawText;
                }
                else if (exportResponse.Data != null)
                {
                    yamlContent = exportResponse.Data;
                }
                else
                {
                    Console.Error.WriteLine($"No export data for {id}");
                    continue;
                }

                var filename = $"{SanitizationHelper.SanitizeFilename(name)}.yml";
                var path = Path.Combine(ExportDir, filename);
                await File.WriteAllTextAsync(path, yamlContent);
                Console.WriteLine($"Saved: {path}");
                await Task.Delay(300);
            }
        }
    }

    /// <summary>
    /// 导入本地应用（原 UploadCommand.RunAsync）。
    /// </summary>
    public static async Task ImportAsync(DifyApiClient client)
    {
        if (!Directory.Exists(ExportDir))
        {
            Console.Error.WriteLine($"import directory '{ExportDir}' does not exist");
            return;
        }

        var processed = 0;
        var files = Directory.GetFiles(ExportDir);

        // 根据 YAML 文件中的 mode 字段判断是否为工作流
        // mode: workflow 为工作流，mode: advanced-chat 等为其他应用
        var workflowFiles = new List<string>();
        var otherFiles = new List<string>();

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);
            if (!string.Equals(ext, ".yml", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ext, ".yaml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var content = await File.ReadAllTextAsync(file);
                var primaryMode = ExtractPrimaryAppMode(content);

                if (IsWorkflowMode(primaryMode))
                {
                    workflowFiles.Add(file);
                }
                else
                {
                    otherFiles.Add(file);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to read {file}: {ex.Message}");
                otherFiles.Add(file); // 出错时默认归类为其他应用
            }
        }
        
        Console.WriteLine($"File Count WorkFlow: {workflowFiles.Count}");
        Console.WriteLine($"File Count NO WorkFlow: {otherFiles.Count}");
        async Task<string?> ImportFileAsync(string path)
        {
            var ext = Path.GetExtension(path);
            if (!string.Equals(ext, ".yml", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ext, ".yaml", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(path);
            var request = new ConsoleApiAppsImportsRequest { YamlContent = content, Mode = "yaml-content" };

            try
            {
                var response = await client.ExecuteConsoleApiAppsImportsAsync(request);
                if (!response.IsSuccessful())
                {
                    Console.Error.WriteLine($"Failed to import {path}: {response.ErrorCode} {response.ErrorMessage ?? response.Error}");
                    return null;
                }
                else
                {
                    Console.WriteLine($"Imported {path} -> appId={response.AppId}, status={response.Status}, task={response.TaskId}");
                    processed++;
                    await Task.Delay(300);
                    return response.AppId;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to import {path}: {e.Message}");
                return null;
            }
        }

        // 存储工作流文件路径和对应的 appId 的映射
        var workflowFileToAppId = new Dictionary<string, string>();
        // 存储工作流文件路径和对应的原始 YAML 内容的映射（用于提取名称）
        var workflowFileToContent = new Dictionary<string, string>();
        // 存储工作流名称到 workflow_tool_id 的映射
        var workflowNameToToolId = new Dictionary<string, string>();

        if (workflowFiles.Count > 0)
        {
            Console.WriteLine($"先上传工作流定义，共 {workflowFiles.Count} 个文件...");
            foreach (var f in workflowFiles)
            {
                // 保存原始内容
                var content = await File.ReadAllTextAsync(f);
                workflowFileToContent[f] = content;
                
                var appId = await ImportFileAsync(f);
                if (!string.IsNullOrEmpty(appId))
                {
                    workflowFileToAppId[f] = appId;
                }
            }

            // 发布工作流并获取 workflow_tool_id
            await PublishWorkflowToolsAsync(client, workflowFileToAppId, workflowFileToContent, workflowNameToToolId);
        }

        if (otherFiles.Count > 0)
        {
            Console.WriteLine($"再上传其他应用，共 {otherFiles.Count} 个文件...");
            // 先更新其他文件中的 provider_id
            if (workflowNameToToolId.Count > 0)
            {
                await ReplaceProviderIdsInOtherFilesAsync(otherFiles, workflowNameToToolId);
            }
            
            foreach (var f in otherFiles)
            {
                await ImportFileAsync(f);
            }
        }

        if (processed == 0)
        {
            Console.WriteLine($"No .yml/.yaml files found in '{ExportDir}'. Nothing was uploaded.");
        }
    }

    /// <summary>
    /// 上传工作流后，自动调用"发布为工具"接口，并在其他文件中替换对应的 provider_id。
    /// </summary>
    private static async Task PublishWorkflowToolsAsync(DifyApiClient client, Dictionary<string, string> workflowFileToAppId, Dictionary<string, string> workflowFileToContent, Dictionary<string, string> workflowNameToToolId)
    {
        if (workflowFileToAppId.Count == 0)
        {
            Console.WriteLine("没有需要发布的工作流。");
            return;
        }

        Console.WriteLine($"开始发布工作流工具，共 {workflowFileToAppId.Count} 个工作流...");

        foreach (var kvp in workflowFileToAppId)
        {
            var filePath = kvp.Key;
            var appId = kvp.Value;
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // 从原始 YAML 文件中提取工作流名称（优先取第一段 app 的 name）
            var workflowName = workflowFileToContent.TryGetValue(filePath, out var yamlContent)
                ? ExtractPrimaryAppName(yamlContent) ?? fileName
                : fileName;

            try
            {
                Console.WriteLine($"处理工作流: {workflowName} (appId: {appId})");

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
                    Console.Error.WriteLine($"Failed to publish workflow {fileName}: {publishResponse.ErrorCode} {publishResponse.ErrorMessage}");
                    continue;
                }

                Console.WriteLine($"  ✓ 工作流已发布");

                // 2. 获取工作流发布信息（包含工具配置）
                var publishGetRequest = new ConsoleApiAppsAppidWorkflowsPublishGetRequest { AppId = appId };
                var publishGetResponse = await client.ExecuteConsoleApiAppsAppidWorkflowsPublishGetAsync(publishGetRequest);
                
                if (!publishGetResponse.IsSuccessful() || publishGetResponse.Data.ValueKind == JsonValueKind.Undefined)
                {
                    Console.Error.WriteLine($"Failed to get workflow publish info for {fileName}");
                    continue;
                }

                // 3. 从发布信息中提取工具配置
                var toolConfig = ExtractToolConfigFromPublishData(publishGetResponse.Data, fileName);
                if (toolConfig == null)
                {
                    Console.WriteLine($"  ⚠ 工作流 {fileName} 未找到工具配置，跳过创建工具");
                    continue;
                }

                // 4. 创建工具
                var createToolRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateRequest
                {
                    Name = toolConfig.Name,
                    Description = toolConfig.Description,
                    Icon = toolConfig.Icon,
                    Label = toolConfig.Label,
                    Parameters = toolConfig.Parameters,
                    Labels = toolConfig.Labels ?? new List<string>(),
                    PrivacyPolicy = toolConfig.PrivacyPolicy ?? "",
                    WorkflowAppId = appId
                };

                var createToolResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowCreateAsync(createToolRequest);
                if (!createToolResponse.IsSuccessful())
                {
                    Console.Error.WriteLine($"Failed to create tool for {fileName}: {createToolResponse.ErrorCode} {createToolResponse.ErrorMessage}");
                    continue;
                }

                Console.WriteLine($"  ✓ 工具已创建");

                // 5. 获取工具 ID
                var getToolRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest { WorkflowAppId = appId };
                var getToolResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(getToolRequest);
                
                if (!getToolResponse.IsSuccessful() || string.IsNullOrEmpty(getToolResponse.WorkflowToolId))
                {
                    Console.Error.WriteLine($"Failed to get tool ID for {fileName}");
                    continue;
                }

                var workflowToolId = getToolResponse.WorkflowToolId;
                Console.WriteLine($"  ✓ 工具 ID: {workflowToolId}");

                // 6. 存储映射关系
                // 使用多个可能的 key 来匹配：label、name、workflowName
                var possibleNames = new List<string>();
                if (!string.IsNullOrEmpty(toolConfig.Label))
                    possibleNames.Add(toolConfig.Label);
                if (!string.IsNullOrEmpty(toolConfig.Name))
                    possibleNames.Add(toolConfig.Name);
                if (!string.IsNullOrEmpty(workflowName))
                    possibleNames.Add(workflowName);
                
                foreach (var name in possibleNames)
                {
                    if (!string.IsNullOrEmpty(name) && !workflowNameToToolId.ContainsKey(name))
                    {
                        workflowNameToToolId[name] = workflowToolId;
                    }
                }
                
                // 也存储文件名作为备用
                if (!workflowNameToToolId.ContainsKey(fileName))
                {
                    workflowNameToToolId[fileName] = workflowToolId;
                }

                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing workflow {fileName}: {ex.Message}");
            }
        }

        Console.WriteLine($"工作流工具发布完成，共获取 {workflowNameToToolId.Count} 个工具 ID。");
    }

    /// <summary>
    /// 从发布数据中提取工具配置信息。
    /// </summary>
    private static WorkflowToolConfig? ExtractToolConfigFromPublishData(JsonElement data, string fileName)
    {
        try
        {
            // 尝试从 data 中提取工具配置
            // 根据实际 API 响应结构调整
            if (data.ValueKind == JsonValueKind.Object)
            {
                // 尝试获取工具相关字段
                if (data.TryGetProperty("tool", out var toolElement) && toolElement.ValueKind == JsonValueKind.Object)
                {
                    var name = toolElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    var description = toolElement.TryGetProperty("description", out var descProp) ? 
                        (descProp.ValueKind == JsonValueKind.Object ? descProp.TryGetProperty("zh_Hans", out var zhProp) ? zhProp.GetString() : null : descProp.GetString()) : null;
                    
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

    /// <summary>
    /// 在其他文件中替换 provider_id。
    /// </summary>
    private static async Task ReplaceProviderIdsInOtherFilesAsync(List<string> files, Dictionary<string, string> workflowNameToToolId)
    {
        if (files.Count == 0 || workflowNameToToolId.Count == 0)
        {
            return;
        }

        Console.WriteLine($"开始在其他文件中替换 provider_id，共 {files.Count} 个文件...");

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var originalContent = content;
                var modified = false;

                // 查找所有 provider_type: workflow 的条目
                var providerPattern = @"provider_name:\s*([^\r\n]+)\s*\r?\n.*?provider_type:\s*workflow\s*\r?\n.*?provider_id:\s*([a-fA-F0-9\-]+)";
                var matches = Regex.Matches(content, providerPattern, RegexOptions.Multiline | RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var providerName = match.Groups[1].Value.Trim().Trim('\'', '"');
                        var oldProviderId = match.Groups[2].Value.Trim();

                        // 尝试匹配 workflowNameToToolId
                        string? newProviderId = null;
                        string? matchedKey = null;
                        
                        foreach (var kvp in workflowNameToToolId)
                        {
                            // 使用模糊匹配：provider_name 包含工作流名称，或工作流名称包含 provider_name
                            if (providerName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Contains(providerName, StringComparison.OrdinalIgnoreCase))
                            {
                                newProviderId = kvp.Value;
                                matchedKey = kvp.Key;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(newProviderId) && newProviderId != oldProviderId)
                        {
                            // 替换 provider_id
                            var oldPattern = $@"(provider_id:\s*){Regex.Escape(oldProviderId)}";
                            content = Regex.Replace(content, oldPattern, $"$1{newProviderId}", RegexOptions.Multiline);
                            modified = true;
                            Console.WriteLine($"  ✓ {Path.GetFileName(file)}: 替换 provider_id {oldProviderId} -> {newProviderId} (provider_name: {providerName}, 匹配到: {matchedKey})");
                        }
                        else if (string.IsNullOrEmpty(newProviderId))
                        {
                            Console.WriteLine($"  ⚠ {Path.GetFileName(file)}: 未找到匹配的工作流工具 (provider_name: {providerName})");
                        }
                    }
                }

                if (modified)
                {
                    await File.WriteAllTextAsync(file, content);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing file {file}: {ex.Message}");
            }
        }
    }


    /// <summary>
    /// 批量发布所有工作流并创建工具。
    /// </summary>
    public static async Task PublishAllWorkflowsAsync(DifyApiClient client)
    {
        Console.WriteLine("\n开始获取所有应用列表...");
        
        // 获取所有应用
        const int pageSize = 30;
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
            if (pageResponse.Data != null && pageResponse.Data.Length > 0)
            {
                allApps.AddRange(pageResponse.Data);
            }

            if (!pageResponse.HasMore || pageResponse.Data == null || pageResponse.Data.Length == 0)
            {
                break;
            }

            listRequest.Page++;
        }

        // 筛选出工作流应用
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
            try
            {
                Console.WriteLine($"处理工作流: {app.Name} (appId: {app.Id})");

                // 1. 发布工作流
                var publishRequest = new ConsoleApiAppsAppidWorkflowsPublishRequest
                {
                    AppId = app.Id,
                    MarkedName = "",
                    MarkedComment = ""
                };

                var publishResponse = await client.ExecuteConsoleApiAppsAppidWorkflowsPublishAsync(publishRequest);
                if (!publishResponse.IsSuccessful())
                {
                    Console.Error.WriteLine($"  ✗ 发布失败: {publishResponse.ErrorCode} {publishResponse.ErrorMessage}");
                    failCount++;
                    continue;
                }

                Console.WriteLine($"  ✓ 工作流已发布");

                // 2. 获取工作流发布信息（包含工具配置）
                var publishGetRequest = new ConsoleApiAppsAppidWorkflowsPublishGetRequest { AppId = app.Id };
                var publishGetResponse = await client.ExecuteConsoleApiAppsAppidWorkflowsPublishGetAsync(publishGetRequest);
                
                if (!publishGetResponse.IsSuccessful() || publishGetResponse.Data.ValueKind == JsonValueKind.Undefined)
                {
                    Console.Error.WriteLine($"  ✗ 获取发布信息失败");
                    failCount++;
                    continue;
                }

                // 3. 从发布信息中提取工具配置
                var toolConfig = ExtractToolConfigFromPublishData(publishGetResponse.Data, app.Name);
                if (toolConfig == null)
                {
                    Console.WriteLine($"  ⚠ 未找到工具配置，跳过创建工具");
                    // 即使没有工具配置，发布也算成功
                    successCount++;
                    await Task.Delay(300);
                    continue;
                }

                // 4. 检查工具是否已存在
                var getToolRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest { WorkflowAppId = app.Id };
                var getToolResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(getToolRequest);
                
                if (getToolResponse.IsSuccessful() && !string.IsNullOrEmpty(getToolResponse.WorkflowToolId))
                {
                    Console.WriteLine($"  ✓ 工具已存在，工具 ID: {getToolResponse.WorkflowToolId}");
                    successCount++;
                    await Task.Delay(300);
                    continue;
                }

                // 5. 创建工具
                var createToolRequest = new ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateRequest
                {
                    Name = toolConfig.Name,
                    Description = toolConfig.Description,
                    Icon = toolConfig.Icon,
                    Label = toolConfig.Label,
                    Parameters = toolConfig.Parameters,
                    Labels = toolConfig.Labels ?? new List<string>(),
                    PrivacyPolicy = toolConfig.PrivacyPolicy ?? "",
                    WorkflowAppId = app.Id
                };

                var createToolResponse = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowCreateAsync(createToolRequest);
                if (!createToolResponse.IsSuccessful())
                {
                    Console.Error.WriteLine($"  ✗ 创建工具失败: {createToolResponse.ErrorCode} {createToolResponse.ErrorMessage}");
                    failCount++;
                    continue;
                }

                Console.WriteLine($"  ✓ 工具已创建");

                // 6. 再次获取工具 ID 确认
                var getToolResponse2 = await client.ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(getToolRequest);
                if (getToolResponse2.IsSuccessful() && !string.IsNullOrEmpty(getToolResponse2.WorkflowToolId))
                {
                    Console.WriteLine($"  ✓ 工具 ID: {getToolResponse2.WorkflowToolId}");
                }

                successCount++;
                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ✗ 处理失败: {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"\n批量发布完成！成功: {successCount}，失败: {failCount}\n");
    }

    /// <summary>
    /// 分析 YAML 文件并输出 Chat 应用与工作流的关系图。
    /// </summary>
    public static async Task AnalyzeChatWorkflowRelationsAsync()
    {
        if (!Directory.Exists(ExportDir))
        {
            Console.Error.WriteLine($"导出目录 '{ExportDir}' 不存在");
            return;
        }

        var fileInfos = await LoadYamlFileInfosAsync();
        if (fileInfos.Count == 0)
        {
            Console.WriteLine($"在 '{ExportDir}' 目录中未找到 YAML 文件。");
            return;
        }

        Console.WriteLine($"\n开始分析 {fileInfos.Count} 个 YAML 文件...\n");

        // 输出分类概览
        Console.WriteLine("YAML 文件分类概览:");
        var modeGroups = fileInfos
            .GroupBy(info => DetermineModeCategory(info.Mode))
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var info in fileInfos)
        {
            var category = DetermineModeCategory(info.Mode);
            var displayName = info.Name ?? "(未命名)";
            var displayMode = info.Mode ?? "(Unknown)";
            Console.WriteLine($"  [{category}] {Path.GetFileName(info.FilePath)} -> 名称: {displayName}, 模式: {displayMode}");
        }

        Console.WriteLine();
        Console.WriteLine("分类统计：");
        Console.WriteLine($"  - Chat: {modeGroups.GetValueOrDefault("CHAT", 0)}");
        Console.WriteLine($"  - Workflow: {modeGroups.GetValueOrDefault("WORKFLOW", 0)}");
        Console.WriteLine($"  - Other/Unknown: {modeGroups.GetValueOrDefault("OTHER", 0)}");
        Console.WriteLine();

        // 只分析 Chat 应用
        var chatFiles = fileInfos.Where(info => IsChatMode(info.Mode)).ToList();
        if (chatFiles.Count == 0)
        {
            Console.WriteLine("未找到任何 Chat 应用或工作流引用。");
            return;
        }

        // 存储 Chat 应用信息：应用名 -> (工作流名 -> 使用次数)
        var chatAppWorkflows = new Dictionary<string, Dictionary<string, int>>();

        foreach (var info in chatFiles)
        {
            try
            {
                var appName = info.Name ?? Path.GetFileNameWithoutExtension(info.FilePath);
                var workflowCounts = GetWorkflowUsageMap(info.Content);
                if (workflowCounts.Count > 0)
                {
                    chatAppWorkflows[appName] = workflowCounts;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"分析文件 {Path.GetFileName(info.FilePath)} 时出错: {ex.Message}");
            }
        }

        if (chatAppWorkflows.Count == 0)
        {
            Console.WriteLine("未在 Chat 应用中找到任何工作流引用。");
            return;
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Chat 应用与工作流关系图");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var totalChatApps = chatAppWorkflows.Count;
        var totalWorkflows = chatAppWorkflows.Values.SelectMany(w => w.Keys).Distinct().Count();
        var totalUsages = chatAppWorkflows.Values.SelectMany(w => w.Values).Sum();

        Console.WriteLine($"统计信息：");
        Console.WriteLine($"  - Chat 应用总数: {totalChatApps}");
        Console.WriteLine($"  - 涉及的工作流总数: {totalWorkflows}");
        Console.WriteLine($"  - 工作流总使用次数: {totalUsages}");
        Console.WriteLine();

        var index = 0;
        foreach (var kvp in chatAppWorkflows.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            index++;
            var appName = kvp.Key;
            var workflows = kvp.Value;

            Console.WriteLine($"【{index}/{totalChatApps}】{appName}");
            Console.WriteLine($"  └─ 涉及工作流数量: {workflows.Count}");

            foreach (var workflow in workflows.OrderBy(w => w.Key, StringComparer.OrdinalIgnoreCase))
            {
                var workflowName = workflow.Key;
                var usageCount = workflow.Value;
                Console.WriteLine($"     • {workflowName} (使用 {usageCount} 次)");
            }

            Console.WriteLine();
        }

        // 输出工作流使用统计（按使用次数排序）
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("工作流使用统计（按使用次数排序）");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var workflowStats = new Dictionary<string, int>();
        foreach (var appWorkflows in chatAppWorkflows.Values)
        {
            foreach (var workflow in appWorkflows)
            {
                if (workflowStats.ContainsKey(workflow.Key))
                {
                    workflowStats[workflow.Key] += workflow.Value;
                }
                else
                {
                    workflowStats[workflow.Key] = workflow.Value;
                }
            }
        }

        var sortedWorkflows = workflowStats.OrderByDescending(w => w.Value).ThenBy(w => w.Key, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var workflow in sortedWorkflows)
        {
            var appsUsingThis = chatAppWorkflows.Count(app => app.Value.ContainsKey(workflow.Key));
            Console.WriteLine($"  {workflow.Key}");
            Console.WriteLine($"    总使用次数: {workflow.Value}");
            Console.WriteLine($"    被 {appsUsingThis} 个 Chat 应用使用");
            Console.WriteLine();
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
    }
    
    /// <summary>
    /// 选择 Chat 应用并复制其自身及依赖的工作流到目标目录。
    /// </summary>
    public static async Task PrepareChatAppPackagesAsync()
    {
        if (!Directory.Exists(ExportDir))
        {
            Console.Error.WriteLine($"导出目录 '{ExportDir}' 不存在");
            return;
        }

        var fileInfos = await LoadYamlFileInfosAsync();
        if (fileInfos.Count == 0)
        {
            Console.WriteLine($"在 '{ExportDir}' 目录中未找到 YAML 文件。");
            return;
        }

        var chatInfos = fileInfos.Where(info => IsChatMode(info.Mode)).OrderBy(info => info.Name ?? Path.GetFileName(info.FilePath), StringComparer.OrdinalIgnoreCase).ToList();
        if (chatInfos.Count == 0)
        {
            Console.WriteLine("未找到任何 Chat 应用。");
            return;
        }

        Console.WriteLine("\n可选 Chat 应用列表：");
        for (int i = 0; i < chatInfos.Count; i++)
        {
            var info = chatInfos[i];
            var displayName = info.Name ?? Path.GetFileNameWithoutExtension(info.FilePath);
            Console.WriteLine($"  {i + 1}. {displayName}  (文件: {Path.GetFileName(info.FilePath)})");
        }

        Console.Write("\n请输入要打包的应用编号（可多个，逗号或空格分隔，输入 0 取消）：");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input) || input == "0")
        {
            Console.WriteLine("已取消操作。");
            return;
        }

        var indices = input.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => int.TryParse(token, out var number) ? number : -1)
            .Where(number => number >= 1 && number <= chatInfos.Count)
            .Distinct()
            .ToList();

        if (indices.Count == 0)
        {
            Console.WriteLine("未选择有效的应用编号。");
            return;
        }

        Console.Write("请输入目标目录（默认 exports_selected）：");
        var targetInput = Console.ReadLine()?.Trim();
        var targetRoot = string.IsNullOrEmpty(targetInput)
            ? Path.Combine(ExportDir, "selected")
            : targetInput;

        Directory.CreateDirectory(targetRoot);

        Console.WriteLine($"\n开始复制，共 {indices.Count} 个 Chat 应用...");
        var totalWorkflowsCopied = 0;

        foreach (var index in indices)
        {
            var info = chatInfos[index - 1];
            var displayName = info.Name ?? Path.GetFileNameWithoutExtension(info.FilePath);
            var sanitizedName = SanitizationHelper.SanitizeFilename(displayName);
            var appTargetDir = Path.Combine(targetRoot, sanitizedName);
            Directory.CreateDirectory(appTargetDir);

            // 复制 Chat 文件
            var chatTargetFile = Path.Combine(appTargetDir, Path.GetFileName(info.FilePath));
            File.Copy(info.FilePath, chatTargetFile, overwrite: true);
            Console.WriteLine($"  ✓ 已复制 Chat 应用：{displayName}");

            // 复制依赖的工作流
            var workflowUsage = GetWorkflowUsageMap(info.Content);
            if (workflowUsage.Count == 0)
            {
                Console.WriteLine("    └─ 未检测到工作流依赖。");
                continue;
            }

            var workflowDir = Path.Combine(appTargetDir, "workflows");
            Directory.CreateDirectory(workflowDir);

            foreach (var workflow in workflowUsage)
            {
                if (TryFindWorkflowFile(fileInfos, workflow.Key, out var workflowInfo) && workflowInfo is not null)
                {
                    var workflowTargetFile = Path.Combine(workflowDir, Path.GetFileName(workflowInfo.FilePath));
                    File.Copy(workflowInfo.FilePath, workflowTargetFile, overwrite: true);
                    Console.WriteLine($"    ├─ 已复制工作流：{workflow.Key} (使用 {workflow.Value} 次)");
                    totalWorkflowsCopied++;
                }
                else
                {
                    Console.WriteLine($"    ├─ ⚠ 找不到工作流文件：{workflow.Key}");
                }
            }
        }

        Console.WriteLine($"\n复制完成！Chat 应用：{indices.Count} 个，工作流文件：{totalWorkflowsCopied} 个。");
        Console.WriteLine($"输出目录：{Path.GetFullPath(targetRoot)}\n");
    }

    /// <summary>
    /// 根据 YAML 内容提取首个 app 块的 mode。
    /// </summary>
    private static string? ExtractPrimaryAppMode(string content)
    {
        using var reader = new StringReader(content);
        string? line;
        var appFound = false;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.Equals("app:", StringComparison.OrdinalIgnoreCase))
            {
                if (!appFound)
                {
                    appFound = true;
                    continue;
                }
                else
                {
                    break;
                }
            }

            if (appFound && trimmed.StartsWith("mode:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring("mode:".Length).Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// 根据 YAML 内容提取首个 app 块的 name。
    /// </summary>
    private static string? ExtractPrimaryAppName(string content)
    {
        using var reader = new StringReader(content);
        string? line;
        var appFound = false;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.Equals("app:", StringComparison.OrdinalIgnoreCase))
            {
                if (!appFound)
                {
                    appFound = true;
                    continue;
                }
                else
                {
                    break;
                }
            }

            if (appFound && trimmed.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring("name:".Length).Trim().Trim('\'', '"');
            }
        }

        return null;
    }

    private static bool IsWorkflowMode(string? mode)
        => string.Equals(mode, "workflow", StringComparison.OrdinalIgnoreCase);

    private static bool IsChatMode(string? mode)
        => string.Equals(mode, "advanced-chat", StringComparison.OrdinalIgnoreCase)
           || string.Equals(mode, "chat", StringComparison.OrdinalIgnoreCase)
           || string.Equals(mode, "agent-chat", StringComparison.OrdinalIgnoreCase);

    private static string DetermineModeCategory(string? mode)
    {
        if (IsChatMode(mode)) return "CHAT";
        if (IsWorkflowMode(mode)) return "WORKFLOW";
        return "OTHER";
    }

    private static Dictionary<string, int> GetWorkflowUsageMap(string content)
    {
        var workflowMatches = Regex.Matches(content, @"provider_type:\s*workflow", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var workflowCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (Match workflowMatch in workflowMatches)
        {
            var startPos = Math.Max(0, workflowMatch.Index - 2000);
            var searchText = content.Substring(startPos, workflowMatch.Index - startPos);
            var nameMatches = Regex.Matches(searchText, @"provider_name:\s*([^\r\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (nameMatches.Count > 0)
            {
                var lastMatch = nameMatches[nameMatches.Count - 1];
                var providerName = lastMatch.Groups[1].Value.Trim().Trim('\'', '"');

                if (!string.IsNullOrEmpty(providerName))
                {
                    if (workflowCounts.ContainsKey(providerName))
                    {
                        workflowCounts[providerName]++;
                    }
                    else
                    {
                        workflowCounts[providerName] = 1;
                    }
                }
            }
        }

        return workflowCounts;
    }

    private static async Task<List<YamlAppFileInfo>> LoadYamlFileInfosAsync()
    {
        var result = new List<YamlAppFileInfo>();
        if (!Directory.Exists(ExportDir))
        {
            return result;
        }

        var files = Directory.GetFiles(ExportDir)
            .Where(f => string.Equals(Path.GetExtension(f), ".yml", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetExtension(f), ".yaml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                result.Add(new YamlAppFileInfo
                {
                    FilePath = file,
                    Content = content,
                    Mode = ExtractPrimaryAppMode(content),
                    Name = ExtractPrimaryAppName(content)
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"读取文件 {Path.GetFileName(file)} 失败: {ex.Message}");
            }
        }

        return result;
    }

    private static bool TryFindWorkflowFile(
        IReadOnlyList<YamlAppFileInfo> fileInfos,
        string providerName,
        out YamlAppFileInfo? workflowInfo)
    {
        workflowInfo = fileInfos.FirstOrDefault(info =>
            IsWorkflowMode(info.Mode) &&
            info.Name is not null &&
            string.Equals(info.Name, providerName, StringComparison.OrdinalIgnoreCase));

        if (workflowInfo is not null)
            return true;

        workflowInfo = fileInfos.FirstOrDefault(info =>
            IsWorkflowMode(info.Mode) &&
            info.Name is not null &&
            info.Name.Contains(providerName, StringComparison.OrdinalIgnoreCase));

        if (workflowInfo is not null)
            return true;

        workflowInfo = fileInfos.FirstOrDefault(info =>
            IsWorkflowMode(info.Mode) &&
            info.Name is not null &&
            providerName.Contains(info.Name, StringComparison.OrdinalIgnoreCase));

        return workflowInfo is not null;
    }

    /// <summary>
    /// 表示解析后的 YAML 文件信息。
    /// </summary>
    private sealed class YamlAppFileInfo
    {
        public required string FilePath { get; init; }
        public required string Content { get; init; }
        public string? Mode { get; init; }
        public string? Name { get; init; }
    }

    /// <summary>
    /// 工作流工具配置信息。
    /// </summary>
    private class WorkflowToolConfig
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Label { get; set; }
        public List<WorkflowToolParameter>? Parameters { get; set; }
        public WorkflowToolIcon? Icon { get; set; }
        public List<string>? Labels { get; set; }
        public string? PrivacyPolicy { get; set; }
    }
}

