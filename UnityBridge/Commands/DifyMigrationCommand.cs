using CsvHelper;
using System.Globalization;
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

        // 先按“workflow”关键字将工作流 YAML 排到前面，再上传其他应用
        var workflowFiles = files
            .Where(f => string.Equals(Path.GetExtension(f), ".yml", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(Path.GetExtension(f), ".yaml", StringComparison.OrdinalIgnoreCase))
            .Where(f => Path.GetFileNameWithoutExtension(f).Contains("workflow", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var otherFiles = files
            .Where(f => string.Equals(Path.GetExtension(f), ".yml", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(Path.GetExtension(f), ".yaml", StringComparison.OrdinalIgnoreCase))
            .Except(workflowFiles)
            .ToArray();
        
        
        Console.WriteLine($"File Count WorkFlow: {workflowFiles.Length}");
        Console.WriteLine($"File Count NO WorkFlow: {otherFiles.Length}");
        async Task ImportFileAsync(string path)
        {
            var ext = Path.GetExtension(path);
            if (!string.Equals(ext, ".yml", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ext, ".yaml", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var content = await File.ReadAllTextAsync(path);
            var request = new ConsoleApiAppsImportsRequest { YamlContent = content, Mode = "yaml-content" };

            try
            {
                var response = await client.ExecuteConsoleApiAppsImportsAsync(request);
                if (!response.IsSuccessful())
                {
                    Console.Error.WriteLine($"Failed to import {path}: {response.ErrorCode} {response.ErrorMessage ?? response.Error}");
                }
                else
                {
                    Console.WriteLine($"Imported {path} -> appId={response.AppId}, status={response.Status}, task={response.TaskId}");
                }
                await Task.Delay(300);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to import {path}: {e.Message}");
            }
            processed++;
        }

        if (workflowFiles.Length > 0)
        {
            Console.WriteLine($"先上传工作流定义，共 {workflowFiles.Length} 个文件...");
            foreach (var f in workflowFiles)
            {
                await ImportFileAsync(f);
            }

            await PublishWorkflowToolsAsync(client, workflowFiles);
        }

        if (otherFiles.Length > 0)
        {
            Console.WriteLine($"再上传其他应用，共 {otherFiles.Length} 个文件...");
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
    /// TODO: 上传工作流后，自动调用“发布为工具”接口。目前缺少接口规格，暂留占位。  并且在 chatflow 种替换对应的工作流为替换后的id
    /// </summary>
    private static Task PublishWorkflowToolsAsync(DifyApiClient client, IReadOnlyList<string> workflowFiles)
    {
        Console.WriteLine("[TODO] 工作流上传完毕，后续将调用“发布为工具”接口（待实现）。");
        return Task.CompletedTask;
    }
}

