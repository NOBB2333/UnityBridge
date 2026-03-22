using System.Text.RegularExpressions;
using UnityBridge.Shared;

namespace UnityBridge.Platforms.Dify;

/// <summary>
/// DifyMigrationCommand - 分析和打包功能。
/// </summary>
public static partial class DifyMigrationCommand
{
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

        Console.WriteLine();
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"  分析 {fileInfos.Count} 个 YAML 文件");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // 输出分类概览
        Console.WriteLine("\n【YAML 文件分类概览】\n");
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        Console.WriteLine("  类型       文件名                              名称");
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        
        var modeGroups = fileInfos
            .GroupBy(info => DetermineModeCategory(info.Mode))
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var info in fileInfos)
        {
            var category = DetermineModeCategory(info.Mode);
            var displayName = info.Name ?? "(未命名)";
            var fileName = Path.GetFileName(info.FilePath);
            var categoryPadded = category.PadRight(10);
            var fileNamePadded = fileName.Length > 35 ? fileName[..32] + "..." : fileName.PadRight(35);
            Console.WriteLine($"  {categoryPadded} {fileNamePadded} {displayName}");
        }

        Console.WriteLine("────────────────────────────────────────────────────────────────");
        Console.WriteLine($"  统计：Chat={modeGroups.GetValueOrDefault("CHAT", 0)}  Workflow={modeGroups.GetValueOrDefault("WORKFLOW", 0)}  Other={modeGroups.GetValueOrDefault("OTHER", 0)}");
        Console.WriteLine("────────────────────────────────────────────────────────────────\n");

        // 只分析 Chat 应用
        var chatFiles = fileInfos.Where(info => IsChatMode(info.Mode)).ToList();
        if (chatFiles.Count == 0)
        {
            Console.WriteLine("未找到任何 Chat 应用或工作流引用。");
            return;
        }

        // 存储 Chat 应用信息：应用名 -> (工作流名 -> 使用次数)
        var chatAppWorkflows = new Dictionary<string, Dictionary<string, int>>();
        // 存储 Chat 应用信息：应用名 -> 插件信息字典
        var chatAppPlugins = new Dictionary<string, Dictionary<string, MarketplacePluginInfo>>();

        foreach (var info in chatFiles)
        {
            try
            {
                var appName = info.Name ?? Path.GetFileNameWithoutExtension(info.FilePath);
                var workflowCounts = GetWorkflowUsageMap(info.Content);
                var pluginMap = GetMarketplacePluginUsageMap(info.Content);
                if (workflowCounts.Count > 0)
                {
                    chatAppWorkflows[appName] = workflowCounts;
                }
                
                if (pluginMap.Count > 0)
                {
                    chatAppPlugins[appName] = pluginMap;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"分析文件 {Path.GetFileName(info.FilePath)} 时出错: {ex.Message}");
            }
        }

        if (chatAppWorkflows.Count == 0 && chatAppPlugins.Count == 0)
        {
            Console.WriteLine("未在 Chat 应用中找到任何工作流引用或插件依赖。");
            return;
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Chat 应用与工作流关系图");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var totalChatApps = Math.Max(chatAppWorkflows.Count, chatAppPlugins.Count);
        var totalWorkflows = chatAppWorkflows.Values.SelectMany(w => w.Keys).Distinct().Count();
        var totalUsages = chatAppWorkflows.Values.SelectMany(w => w.Values).Sum();
        var totalPlugins = chatAppPlugins.Values.SelectMany(p => p.Keys).Distinct().Count();

        Console.WriteLine($"统计信息：");
        Console.WriteLine($"  - Chat 应用总数: {totalChatApps}");
        Console.WriteLine($"  - 涉及的工作流总数: {totalWorkflows}");
        Console.WriteLine($"  - 工作流总使用次数: {totalUsages}");
        Console.WriteLine($"  - 涉及的插件市场插件总数: {totalPlugins}");
        Console.WriteLine();

        // 合并所有应用（有工作流或插件的）
        var allApps = new HashSet<string>(chatAppWorkflows.Keys);
        foreach (var app in chatAppPlugins.Keys)
        {
            allApps.Add(app);
        }

        var index = 0;
        foreach (var appName in allApps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            index++;
            var hasWorkflows = chatAppWorkflows.TryGetValue(appName, out var workflows);
            var hasPlugins = chatAppPlugins.TryGetValue(appName, out var plugins);

            Console.WriteLine($"【{index}/{allApps.Count}】{appName}");

            if (hasWorkflows && workflows != null && workflows.Count > 0)
            {
                Console.WriteLine($"  ├─ 涉及工作流数量: {workflows.Count}");
                foreach (var workflow in workflows.OrderBy(w => w.Key, StringComparer.OrdinalIgnoreCase))
                {
                    var workflowName = workflow.Key;
                    var usageCount = workflow.Value;
                    Console.WriteLine($"  │  • {workflowName} (使用 {usageCount} 次)");
                }
            }

            if (hasPlugins && plugins != null && plugins.Count > 0)
            {
                var prefix = hasWorkflows && workflows != null && workflows.Count > 0 ? "  ├─" : "  └─";
                Console.WriteLine($"{prefix} 涉及插件市场插件数量: {plugins.Count}");
                foreach (var plugin in plugins.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    var pluginInfo = plugin.Value;
                    var displayName = !string.IsNullOrWhiteSpace(pluginInfo.Title)
                        ? pluginInfo.Title!
                        : (!string.IsNullOrWhiteSpace(pluginInfo.ProviderName) ? pluginInfo.ProviderName : pluginInfo.ProviderId);
                    var toolNamesStr = pluginInfo.ToolNames.Count > 0 
                        ? $" (工具: {string.Join(", ", pluginInfo.ToolNames)})" 
                        : "";
                    Console.WriteLine($"  │  • {displayName} (ID: {pluginInfo.ProviderId}, 使用 {pluginInfo.UsageCount} 次{toolNamesStr})");
                }
            }

            if (!hasWorkflows && (!hasPlugins || plugins == null || plugins.Count == 0))
            {
                Console.WriteLine($"  └─ 无工作流或插件依赖");
            }

            Console.WriteLine();
        }

        // 输出工作流使用统计（按使用次数排序）
        if (totalWorkflows > 0)
        {
            PrintWorkflowStats(chatAppWorkflows);
        }

        // 输出插件市场插件使用统计（按使用次数排序）
        if (totalPlugins > 0)
        {
            PrintPluginStats(chatAppPlugins);
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
    }

    private static void PrintWorkflowStats(Dictionary<string, Dictionary<string, int>> chatAppWorkflows)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("工作流使用统计（按使用次数排序）");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var workflowStats = new Dictionary<string, int>();
        foreach (var appWorkflows in chatAppWorkflows.Values)
        {
            foreach (var workflow in appWorkflows)
            {
                if (workflowStats.ContainsKey(workflow.Key))
                    workflowStats[workflow.Key] += workflow.Value;
                else
                    workflowStats[workflow.Key] = workflow.Value;
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
    }

    private static void PrintPluginStats(Dictionary<string, Dictionary<string, MarketplacePluginInfo>> chatAppPlugins)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("插件市场插件使用统计（按使用次数排序）");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var pluginStats = new Dictionary<string, MarketplacePluginInfo>();
        foreach (var appPlugins in chatAppPlugins.Values)
        {
            foreach (var plugin in appPlugins)
            {
                if (pluginStats.ContainsKey(plugin.Key))
                {
                    pluginStats[plugin.Key].UsageCount += plugin.Value.UsageCount;
                    if (pluginStats[plugin.Key].Title is null && !string.IsNullOrEmpty(plugin.Value.Title))
                        pluginStats[plugin.Key].Title = plugin.Value.Title;
                    foreach (var toolName in plugin.Value.ToolNames)
                    {
                        if (!pluginStats[plugin.Key].ToolNames.Contains(toolName))
                            pluginStats[plugin.Key].ToolNames.Add(toolName);
                    }
                }
                else
                {
                    pluginStats[plugin.Key] = new MarketplacePluginInfo
                    {
                        ProviderId = plugin.Value.ProviderId,
                        ProviderName = plugin.Value.ProviderName,
                        Title = plugin.Value.Title,
                        ProviderType = plugin.Value.ProviderType,
                        UsageCount = plugin.Value.UsageCount,
                        ToolNames = new List<string>(plugin.Value.ToolNames)
                    };
                }
            }
        }

        var sortedPlugins = pluginStats.OrderByDescending(p => p.Value.UsageCount).ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var plugin in sortedPlugins)
        {
            var pluginInfo = plugin.Value;
            var appsUsingThis = chatAppPlugins.Where(app => app.Value.ContainsKey(plugin.Key)).ToList();
            var displayName = !string.IsNullOrWhiteSpace(pluginInfo.Title)
                ? pluginInfo.Title!
                : (!string.IsNullOrWhiteSpace(pluginInfo.ProviderName) ? pluginInfo.ProviderName : pluginInfo.ProviderId);
            var toolNamesStr = pluginInfo.ToolNames.Count > 0 
                ? $" (工具: {string.Join(", ", pluginInfo.ToolNames)})" 
                : "";
            
            Console.WriteLine($"  {displayName}");
            Console.WriteLine($"    插件 ID: {pluginInfo.ProviderId}");
            Console.WriteLine($"    类型: {pluginInfo.ProviderType}");
            Console.WriteLine($"    总使用次数: {pluginInfo.UsageCount}{toolNamesStr}");
            Console.WriteLine($"    被 {appsUsingThis.Count} 个 Chat 应用使用");
            if (appsUsingThis.Count > 0)
            {
                var appNames = appsUsingThis.Select(app => app.Key).OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
                Console.WriteLine($"    使用该插件的应用: {string.Join(", ", appNames)}");
            }
            Console.WriteLine();
        }
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

        Console.WriteLine();
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("  可选 Chat 应用列表");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("  编号   应用名称                        文件名");
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        
        for (int i = 0; i < chatInfos.Count; i++)
        {
            var info = chatInfos[i];
            var displayName = info.Name ?? Path.GetFileNameWithoutExtension(info.FilePath);
            var fileName = Path.GetFileName(info.FilePath);
            var numPadded = $"{i + 1})".PadRight(6);
            var namePadded = displayName.Length > 30 ? displayName[..27] + "..." : displayName.PadRight(30);
            Console.WriteLine($"  {numPadded} {namePadded} {fileName}");
        }
        
        Console.WriteLine("────────────────────────────────────────────────────────────────");
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

        Console.WriteLine();
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        Console.WriteLine($"  开始复制，共 {indices.Count} 个 Chat 应用");
        Console.WriteLine("────────────────────────────────────────────────────────────────");
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
            Console.WriteLine($"\n  ✓ Chat 应用：{displayName}");

            // 复制依赖的工作流
            var workflowUsage = GetWorkflowUsageMap(info.Content);
            if (workflowUsage.Count == 0)
            {
                Console.WriteLine("      └─ 无工作流依赖");
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
                    Console.WriteLine($"      ├─ ✓ {workflow.Key} (使用 {workflow.Value} 次)");
                    totalWorkflowsCopied++;
                }
                else
                {
                    Console.WriteLine($"      ├─ ⚠ 未找到：{workflow.Key}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        Console.WriteLine($"  复制完成！Chat 应用：{indices.Count} 个，工作流：{totalWorkflowsCopied} 个");
        Console.WriteLine($"  输出目录：{Path.GetFullPath(targetRoot)}");
        Console.WriteLine("────────────────────────────────────────────────────────────────\n");
    }

    // Mode/Name 提取方法已移至 YamlParsingHelper
    private static string? ExtractPrimaryAppMode(string content) => YamlParsingHelper.ExtractPrimaryAppMode(content);
    private static string? ExtractPrimaryAppName(string content) => YamlParsingHelper.ExtractPrimaryAppName(content);
    private static bool IsWorkflowMode(string? mode) => YamlParsingHelper.IsWorkflowMode(mode);
    private static bool IsChatMode(string? mode) => YamlParsingHelper.IsChatMode(mode);
    private static string DetermineModeCategory(string? mode) => YamlParsingHelper.DetermineModeCategory(mode);

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
                        workflowCounts[providerName]++;
                    else
                        workflowCounts[providerName] = 1;
                }
            }
        }

        return workflowCounts;
    }

    /// <summary>
    /// 检测字符串是否是 UUID 格式。
    /// </summary>
    private static bool IsUuid(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        return Regex.IsMatch(value, @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 提取插件市场插件使用情况。
    /// </summary>
    private static Dictionary<string, MarketplacePluginInfo> GetMarketplacePluginUsageMap(string content)
    {
        var pluginMap = new Dictionary<string, MarketplacePluginInfo>(StringComparer.OrdinalIgnoreCase);
        string? NormalizeValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Trim('\'', '"');

        var builtinMatches = Regex.Matches(content, @"provider_type:\s*builtin", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        
        foreach (Match builtinMatch in builtinMatches)
        {
            var blockStart = Math.Max(0, builtinMatch.Index - 200);
            var blockEnd = Math.Min(content.Length, builtinMatch.Index + 1000);
            var pluginBlock = content.Substring(blockStart, blockEnd - blockStart);

            var providerIdMatch = Regex.Match(pluginBlock, @"provider_id:\s*([^\r\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (!providerIdMatch.Success) continue;

            var providerId = NormalizeValue(providerIdMatch.Groups[1].Value);
            if (string.IsNullOrEmpty(providerId)) continue;
            if (!providerId.Contains('/') || IsUuid(providerId)) continue;

            var providerNameMatch = Regex.Match(pluginBlock, @"provider_name:\s*([^\r\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var providerName = providerNameMatch.Success ? NormalizeValue(providerNameMatch.Groups[1].Value) : providerId;

            var titleMatch = Regex.Match(pluginBlock, @"title:\s*([^\r\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var title = titleMatch.Success ? NormalizeValue(titleMatch.Groups[1].Value) : null;

            var toolNameMatch = Regex.Match(pluginBlock, @"tool_name:\s*([^\r\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var toolName = toolNameMatch.Success ? NormalizeValue(toolNameMatch.Groups[1].Value) : null;

            if (pluginMap.ContainsKey(providerId))
            {
                pluginMap[providerId].UsageCount++;
                if (pluginMap[providerId].Title is null && !string.IsNullOrEmpty(title))
                    pluginMap[providerId].Title = title;
                if (!string.IsNullOrEmpty(toolName) && !pluginMap[providerId].ToolNames.Contains(toolName))
                    pluginMap[providerId].ToolNames.Add(toolName);
            }
            else
            {
                pluginMap[providerId] = new MarketplacePluginInfo
                {
                    ProviderId = providerId,
                    ProviderName = providerName,
                    Title = title,
                    ProviderType = "builtin",
                    UsageCount = 1,
                    ToolNames = string.IsNullOrEmpty(toolName) ? new List<string>() : new List<string> { toolName }
                };
            }
        }

        return pluginMap;
    }

    /// <summary>
    /// 插件市场插件信息。
    /// </summary>
    private class MarketplacePluginInfo
    {
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string ProviderType { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public List<string> ToolNames { get; set; } = new();
    }

    private static async Task<List<YamlAppFileInfo>> LoadYamlFileInfosAsync()
    {
        var result = new List<YamlAppFileInfo>();
        if (!Directory.Exists(ExportDir)) return result;

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

    private static bool TryFindWorkflowFile(IReadOnlyList<YamlAppFileInfo> fileInfos, string providerName, out YamlAppFileInfo? workflowInfo)
    {
        workflowInfo = fileInfos.FirstOrDefault(info =>
            IsWorkflowMode(info.Mode) && info.Name is not null &&
            string.Equals(info.Name, providerName, StringComparison.OrdinalIgnoreCase));

        if (workflowInfo is not null) return true;

        workflowInfo = fileInfos.FirstOrDefault(info =>
            IsWorkflowMode(info.Mode) && info.Name is not null &&
            info.Name.Contains(providerName, StringComparison.OrdinalIgnoreCase));

        if (workflowInfo is not null) return true;

        workflowInfo = fileInfos.FirstOrDefault(info =>
            IsWorkflowMode(info.Mode) && info.Name is not null &&
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
}
