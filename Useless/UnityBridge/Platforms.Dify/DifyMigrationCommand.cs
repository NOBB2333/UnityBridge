using System.Text.Json;
using System.Text.RegularExpressions;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;
using UnityBridge.Shared;

namespace UnityBridge.Platforms.Dify;

/// <summary>
/// 包含 Dify 导入导出相关命令。
/// </summary>
public static partial class DifyMigrationCommand
{
    private const string ExportDir = "exports2";

    /// <summary>
    /// 导出所有应用。
    /// </summary>
    public static async Task ExportAsync(DifyApiClient client)
    {
        var allApps = await DifyApiHelper.FetchAllAppsAsync(client, 30, (page, count) =>
        {
            Console.WriteLine($"已获取应用列表第 {page} 页，累计 {count} 条...");
        });

        CsvExportHelper.EnsureDirectoryExists(ExportDir);

        Console.WriteLine($"data count: {allApps.Count}");

        var csvRows = new List<AppCsvRow>(allApps.Count);
        foreach (var app in allApps)
        {
            csvRows.Add(new AppCsvRow
            {
                Id = app.Id,
                Name = app.Name,
                Description = app.Description,
                Mode = app.Mode,
                IconType = app.IconType,
                Icon = app.Icon,
                IconBackground = app.IconBackground,
                IconUrl = app.IconUrl,
                Tags = FormatTags(app.Tags),
                MaxActiveRequests = app.MaxActiveRequests,
                ModelConfig = app.ModelConfig.HasValue ? app.ModelConfig.Value.GetRawText() : null,
                WorkflowId = app.Workflow?.Id,
                WorkflowCreatedBy = app.Workflow?.CreatedBy,
                WorkflowCreatedAt = app.Workflow?.CreatedAt,
                WorkflowUpdatedBy = app.Workflow?.UpdatedBy,
                WorkflowUpdatedAt = app.Workflow?.UpdatedAt,
                UseIconAsAnswerIcon = app.UseIconAsAnswerIcon,
                CreatedBy = app.CreatedBy,
                CreatedAt = app.CreatedAt,
                UpdatedBy = app.UpdatedBy,
                UpdatedAt = app.UpdatedAt,
                AccessMode = app.AccessMode,
                CreateUserName = app.CreateUserName,
                AuthorName = app.AuthorName
            });
        }

        var csvPath = CsvExportHelper.Export(csvRows, ExportDir, "apps.csv");
        Console.WriteLine($"Saved app list CSV: {csvPath}");

        foreach (var app in allApps)
        {
            var exportRequest = new ConsoleApiAppsAppidExportRequest { AppId = app.Id, IncludeSecret = false };
            var exportResponse = await client.ExecuteConsoleApiAppsAppidExportAsync(exportRequest);

            string? yamlContent = exportResponse.RawText ?? exportResponse.Data;
            if (string.IsNullOrEmpty(yamlContent))
            {
                Console.Error.WriteLine($"No export data for {app.Id}");
                continue;
            }

            var filename = $"{SanitizationHelper.SanitizeFilename(app.Name)}.yml";
            var path = Path.Combine(ExportDir, filename);
            await File.WriteAllTextAsync(path, yamlContent);
            Console.WriteLine($"Saved: {path}");
            await Task.Delay(300);
        }
    }

    /// <summary>
    /// 导入本地应用。
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

        var workflowFiles = new List<string>();
        var otherFiles = new List<string>();

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);
            if (!ext.Equals(".yml", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var content = await File.ReadAllTextAsync(file);
                var primaryMode = YamlParsingHelper.ExtractPrimaryAppMode(content);

                if (YamlParsingHelper.IsWorkflowMode(primaryMode))
                    workflowFiles.Add(file);
                else
                    otherFiles.Add(file);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to read {file}: {ex.Message}");
                otherFiles.Add(file);
            }
        }
        
        Console.WriteLine($"File Count WorkFlow: {workflowFiles.Count}");
        Console.WriteLine($"File Count NO WorkFlow: {otherFiles.Count}");

        async Task<string?> ImportFileAsync(string path)
        {
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
                
                Console.WriteLine($"Imported {path} -> appId={response.AppId}, status={response.Status}");
                if (!string.IsNullOrEmpty(response.TaskId))
                    await CheckDependenciesAsync(client, response.TaskId);
                
                processed++;
                await Task.Delay(300);
                return response.AppId;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to import {path}: {e.Message}");
                return null;
            }
        }

        var workflowFileToAppId = new Dictionary<string, string>();
        var workflowFileToContent = new Dictionary<string, string>();
        var workflowNameToToolId = new Dictionary<string, string>();

        if (workflowFiles.Count > 0)
        {
            Console.WriteLine($"先上传工作流定义，共 {workflowFiles.Count} 个文件...");
            foreach (var f in workflowFiles)
            {
                var content = await File.ReadAllTextAsync(f);
                workflowFileToContent[f] = content;
                
                var appId = await ImportFileAsync(f);
                if (!string.IsNullOrEmpty(appId))
                    workflowFileToAppId[f] = appId;
            }

            await PublishWorkflowToolsAsync(client, workflowFileToAppId, workflowFileToContent, workflowNameToToolId);
        }

        if (otherFiles.Count > 0)
        {
            Console.WriteLine($"再上传其他应用，共 {otherFiles.Count} 个文件...");
            if (workflowNameToToolId.Count > 0)
                await ReplaceProviderIdsInOtherFilesAsync(otherFiles, workflowNameToToolId);
            
            foreach (var f in otherFiles)
                await ImportFileAsync(f);
        }

        if (processed == 0)
            Console.WriteLine($"No .yml/.yaml files found in '{ExportDir}'.");
    }

    private static async Task CheckDependenciesAsync(DifyApiClient client, string taskId)
    {
        try
        {
            var checkRequest = new ConsoleApiAppsImportsCheckDependenciesRequest { ImportId = taskId };
            var checkResponse = await client.ExecuteConsoleApiAppsImportsCheckDependenciesAsync(checkRequest);
            
            if (checkResponse.IsSuccessful() && checkResponse.LeakedDependencies?.Count > 0)
            {
                Console.WriteLine($"  Warning: Found {checkResponse.LeakedDependencies.Count} leaked dependencies");
            }
            else if (checkResponse.IsSuccessful())
            {
                Console.WriteLine($"  Dependencies check passed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Exception checking dependencies: {ex.Message}");
        }
    }

    private static async Task PublishWorkflowToolsAsync(
        DifyApiClient client,
        Dictionary<string, string> workflowFileToAppId,
        Dictionary<string, string> workflowFileToContent,
        Dictionary<string, string> workflowNameToToolId)
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
            
            var workflowName = workflowFileToContent.TryGetValue(filePath, out var yamlContent)
                ? YamlParsingHelper.ExtractPrimaryAppName(yamlContent) ?? fileName
                : fileName;

            var toolId = await DifyWorkflowPublishCommand.PublishSingleWorkflowAsync(client, appId, workflowName);
            if (!string.IsNullOrEmpty(toolId))
            {
                if (!string.IsNullOrEmpty(workflowName) && !workflowNameToToolId.ContainsKey(workflowName))
                    workflowNameToToolId[workflowName] = toolId;
                if (!workflowNameToToolId.ContainsKey(fileName))
                    workflowNameToToolId[fileName] = toolId;
            }

            await Task.Delay(300);
        }

        Console.WriteLine($"工作流工具发布完成，共获取 {workflowNameToToolId.Count} 个工具 ID。");
    }

    private static async Task ReplaceProviderIdsInOtherFilesAsync(List<string> files, Dictionary<string, string> workflowNameToToolId)
    {
        Console.WriteLine($"开始在其他文件中替换 provider_id，共 {files.Count} 个文件...");

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var modified = false;

                var providerPattern = @"provider_name:\s*([^\r\n]+)\s*\r?\n.*?provider_type:\s*workflow\s*\r?\n.*?provider_id:\s*([a-fA-F0-9\-]+)";
                var matches = Regex.Matches(content, providerPattern, RegexOptions.Multiline | RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var providerName = match.Groups[1].Value.Trim().Trim('\'', '"');
                        var oldProviderId = match.Groups[2].Value.Trim();

                        string? newProviderId = workflowNameToToolId
                            .FirstOrDefault(kvp => 
                                providerName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Contains(providerName, StringComparison.OrdinalIgnoreCase))
                            .Value;

                        if (!string.IsNullOrEmpty(newProviderId) && newProviderId != oldProviderId)
                        {
                            content = Regex.Replace(content, $@"(provider_id:\s*){Regex.Escape(oldProviderId)}", $"$1{newProviderId}", RegexOptions.Multiline);
                            modified = true;
                            Console.WriteLine($"  ✓ {Path.GetFileName(file)}: 替换 provider_id");
                        }
                    }
                }

                if (modified)
                    await File.WriteAllTextAsync(file, content);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing file {file}: {ex.Message}");
            }
        }
    }

    private static string? FormatTags(JsonElement[]? tags)
    {
        if (tags is not { Length: > 0 }) return null;
        var list = tags.Select(tag => tag.ValueKind == JsonValueKind.String ? tag.GetString() : tag.GetRawText())
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        return list.Count == 0 ? null : string.Join('|', list!);
    }

    private sealed class AppCsvRow
    {
        public string Id { get; init; } = string.Empty;
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Mode { get; init; }
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
    }
}
