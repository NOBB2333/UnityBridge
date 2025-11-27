namespace UnityBridge;

using Flurl.Http;
using UnityBridge.Api.Dify;
using UnityBridge.Commands;
using UnityBridge.Helpers;


class Program
{
    // 下载
    private const string BaseUrl = "http://172.30.16.66:88";
    private const string HeaderSpec = @"
    'Accept': '*/*',
    'Accept-Language': 'zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6',
    'Cache-Control': 'no-cache',
    'Connection': 'keep-alive',
    'Pragma': 'no-cache',
    'Referer': 'http://172.30.16.66:88/apps?category=workflow',
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36 Edg/142.0.0.0',
    'authorization': 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiNmNkYzZjYmYtYjliMy00NjU3LWE5OWMtYjQwNGIyZDFmNmQzIiwiZXhwIjoxNzYzOTc1MzA0LCJpc3MiOiJTRUxGX0hPU1RFRCIsInN1YiI6IkNvbnNvbGUgQVBJIFBhc3Nwb3J0In0.iGkFi7fS0v0C0a8qZVuX0updg6t1pt7NHA-6AFelB_c',
    'content-type': 'application/json',
    'Cookie': 'locale=zh-Hans; Hm_lvt_b7fd400b2e66412997802961d8148bce=1763445635; Token=89edade0-f643-4ecc-a00c-bcbfc4a76c2a',
    ";

    static async Task Main(string[] args)
    {
        var headers = HeaderHelper.ParseHeaders(HeaderSpec);
        var options = new DifyApiClientOptions
        {
            Endpoint = BaseUrl,
            Timeout = 60000 // 60 seconds
        };

        //  var client = new DifyApiClient(options);   改成下面的表达式生成器
        // Configure JSON Source Generation for Native AOT
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        var client = new DifyApiClient(options, null, true, jsonOptions);
        
        // Apply headers to the underlying FlurlClient
        foreach (var header in headers)
        {
            client.FlurlClient.WithHeader(header.Key, header.Value);
        }

        var menuOptions = new[] 
        {
            "1) 下载 (导出) 应用",
            "2) 上传 (导入) 应用",
            "3) 检测文件/文件夹编码 (Not Implemented)",
            "4) 生成 API Key",
            "5) 获取应用详情",
            "6) Test URLHelper"
        };

        var commands = new Dictionary<string, Func<Task>>
        {
            ["1"] = () => DownloadCommand.RunAsync(client),
            ["2"] = () => UploadCommand.RunAsync(client),
            ["3"] = () => { Console.WriteLine("检测文件/文件夹编码 功能尚未移植。"); return Task.CompletedTask; },
            ["4"] = () => GenerateKeyCommand.RunAsync(client),
            ["5"] = () => GetAppInfoCommand.RunAsync(client),
            ["6"] = () => { UnityBridge.Test.TestURLHelper.Run(); return Task.CompletedTask; }
        };

        while (true)
        {
            Console.WriteLine($"请选择操作:\n{string.Join('\n', menuOptions)}");
            Console.Write("输入选项编号后回车: ");

            var choice = Console.ReadLine()?.Trim();
            if (commands.TryGetValue(choice ?? "", out var command))
                await command();
            else
                Console.WriteLine("无效选项,请重新输入。\n");
        }
    }
}