using CsvHelper;
using System.Globalization;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;
using UnityBridge.Helpers;

namespace UnityBridge.Commands;

public static class DownloadCommand
{
    private const string ExportDir = "exports";

    public static async Task RunAsync(DifyApiClient client)
    {
        var listRequest = new ConsoleApiAppsRequest { Page = 1, Limit = 999 };
        var listResponse = await client.ExecuteConsoleApiAppsAsync(listRequest);

        if (!Directory.Exists(ExportDir))
        {
            Directory.CreateDirectory(ExportDir);
        }

        Console.WriteLine($"data count: {listResponse.Data?.Length ?? 0}");
        var csvPath = Path.Combine(ExportDir, "apps.csv");

        using (var writer = new StreamWriter(csvPath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            if (listResponse.Data != null)
            {
                csv.WriteRecords(listResponse.Data);
            }
        }
        Console.WriteLine($"Saved app list CSV: {csvPath}");

        if (listResponse.Data != null)
        {
            foreach (var app in listResponse.Data)
            {
                var id = app.Id;
                var name = app.Name;

                var exportRequest = new ConsoleApiAppsAppidExportRequest { AppId = id, IncludeSecret = false };
                var exportResponse = await client.ExecuteConsoleApiAppsAppidExportAsync(exportRequest);

                string yamlContent;
                if (!string.IsNullOrEmpty(exportResponse.RawText))
                {
                     // Simple normalization if needed, for now just use raw text
                     yamlContent = exportResponse.RawText;
                }
                else if (exportResponse.Data != null)
                {
                     yamlContent = System.Text.Json.JsonSerializer.Serialize(exportResponse.Data);
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
}
