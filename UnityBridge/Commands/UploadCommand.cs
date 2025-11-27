using UnityBridge.Helpers;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;

namespace UnityBridge.Commands;

public static class UploadCommand
{
    private const string ImportDir = "exports";

    public static async Task RunAsync(DifyApiClient client)
    {
        if (!Directory.Exists(ImportDir))
        {
            Console.Error.WriteLine($"import directory '{ImportDir}' does not exist");
            return;
        }

        var processed = 0;
        var files = Directory.GetFiles(ImportDir);

        foreach (var path in files)
        {
            var ext = Path.GetExtension(path);
            if (!string.Equals(ext, ".yml", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ext, ".yaml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(path);
            var request = new ConsoleApiAppsImportsRequest { YamlContent = content, Mode = "yaml-content" };

            try
            {
                await client.ExecuteConsoleApiAppsImportsAsync(request);
                Console.WriteLine($"Imported {path}");
                await Task.Delay(300);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to import {path}: {e.Message}");
            }
            processed++;
        }

        if (processed == 0)
        {
            Console.WriteLine($"No .yml/.yaml files found in '{ImportDir}'. Nothing was uploaded.");
        }
    }
}
