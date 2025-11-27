using CsvHelper;
using System.Globalization;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;
using UnityBridge.Helpers; // Added this line

namespace UnityBridge.Commands;

public static class GetAppInfoCommand
{
    private const string ExportDir = "exports";

    public static async Task RunAsync(DifyApiClient client)
    {
        while (true)
        {
            Console.WriteLine("\n请选择操作:");
            Console.WriteLine("1) 通过应用 ID 获取应用详情");
            Console.WriteLine("2) 从应用列表中选择并查看详情");
            Console.WriteLine("3) 下载全部应用详情并导出 CSV");
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
                    await DownloadAllAppsAsync(client);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("无效选项，请重新输入。\n");
                    break;
            }
        }
    }

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

            if (choice == "0") return null;

            if (int.TryParse(choice, out int num) && num > 0 && num <= listResponse.Data.Length)
            {
                return listResponse.Data[num - 1].Id;
            }
            Console.WriteLine("无效选项，请重新输入。");
        }
    }

    private static async Task GetAppInfoAsync(DifyApiClient client, string appId)
    {
        var request = new ConsoleApiAppsAppidRequest { AppId = appId };
        var response = await client.ExecuteConsoleApiAppsAppidAsync(request);

        Console.WriteLine("\n应用信息:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
    }

    private static async Task DownloadAllAppsAsync(DifyApiClient client)
    {
        Console.WriteLine("\n开始下载所有应用详情并导出 CSV...");
        int page = 1;
        int limit = 100;
        var collectedApps = new List<ConsoleApiAppsAppidResponse>();

        while (true)
        {
            var listRequest = new ConsoleApiAppsRequest { Page = page, Limit = limit };
            var listResponse = await client.ExecuteConsoleApiAppsAsync(listRequest);

            if (listResponse.Data == null || listResponse.Data.Length == 0)
            {
                break;
            }

            foreach (var app in listResponse.Data)
            {
                var request = new ConsoleApiAppsAppidRequest { AppId = app.Id };
                try
                {
                    var detail = await client.ExecuteConsoleApiAppsAppidAsync(request);
                    Console.WriteLine($"获取应用详情: {app.Name} ({app.Id})");
                    collectedApps.Add(detail);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"获取 {app.Name} ({app.Id}) 详情失败: {ex.Message}");
                }
                await Task.Delay(300);
            }

            if (listResponse.HasMore != true)
            {
                break;
            }
            page++;
        }

        if (collectedApps.Count == 0)
        {
            Console.WriteLine("未拉取到任何应用详情，已取消导出。");
            return;
        }

        if (!Directory.Exists(ExportDir))
        {
            Directory.CreateDirectory(ExportDir);
        }

        var csvPath = Path.Combine(ExportDir, "apps_detail.csv");
        using (var writer = new StreamWriter(csvPath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(collectedApps);
        }
        Console.WriteLine($"已保存 {collectedApps.Count} 条应用详情到 {csvPath}");
    }
}
