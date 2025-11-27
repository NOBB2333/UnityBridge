using UnityBridge.Helpers;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Dify.Extensions;
using UnityBridge.Api.Dify.Models;

namespace UnityBridge.Commands;

public static class GenerateKeyCommand
{
    public static async Task RunAsync(DifyApiClient client)
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
        if (response.Data.CreatedAt != null)
        {
            Console.WriteLine($"Created:   {response.Data.CreatedAt}");
        }
        
        var lastUsedStr = response.Data.LastUsedAt; 
        if (string.IsNullOrEmpty(lastUsedStr))
        {
             Console.WriteLine("Last Used: 从未使用");
        }
        else
        {
            Console.WriteLine($"Last Used: {lastUsedStr}");
        }
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        await Task.Delay(300);
    }
}
