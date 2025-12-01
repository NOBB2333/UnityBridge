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
        // 试用期检测 - 必须在程序启动时最先执行
        if (!TrialManager.CheckTrialStatus())
        {
            return; // 试用期已过期且未激活，退出程序
        }

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

        // 菜单选项定义
        var menuOptions = new[] 
        {
            "1) 下载 (导出) 应用",
            "2) 上传 (导入) 应用",
            "3) 检测文件/文件夹编码",
            "4) 生成 API Key",
            "5) 获取应用详情",
            "6) 查看试用期信息",
            "7) 测试本机授权 (显示机器码并生成测试激活 key)"
        };

        // 菜单选项与命令方法的映射关系
        // 选项 1 -> DownloadCommand.RunAsync
        // 选项 2 -> UploadCommand.RunAsync
        // 选项 3 -> DetectEncodingCommand.RunAsync
        // 选项 4 -> GenerateKeyCommand.RunAsync
        // 选项 5 -> GetAppInfoCommand.RunAsync
        // 选项 6 -> TrialManager.ShowTrialInfo
        // 选项 7 -> 本机授权测试 (打印机器码 + 生成并应用测试激活 key)
        var commands = new Dictionary<string, Func<Task>>
        {
            ["1"] = async () => await DownloadCommand.RunAsync(client),      // 下载 (导出) 应用
            ["2"] = async () => await UploadCommand.RunAsync(client),        // 上传 (导入) 应用
            ["3"] = async () => await DetectEncodingCommand.RunAsync(),      // 检测文件/文件夹编码
            ["4"] = async () => await GenerateKeyCommand.RunAsync(client),  // 生成 API Key
            ["5"] = async () => await GetAppInfoCommand.RunAsync(client),   // 获取应用详情
            ["6"] = () => { TrialManager.ShowTrialInfo(); return Task.CompletedTask; }, // 查看试用期信息
            ["7"] = () =>                                                   // 本机授权测试
            {
                Console.WriteLine("\n========================================");
                Console.WriteLine("本机授权测试");
                Console.WriteLine("========================================");
                var machineCode = TrialManager.GetMachineCode();
                Console.WriteLine($"机器码：{machineCode}");
                Console.WriteLine("说明：将此机器码发给作者，可由作者生成正式激活 key。");

                // 为了本机自测，直接生成一个延长 365 天的测试 key，并立即尝试激活
                var testKey = TrialManager.GenerateActivationKey(365);
                Console.WriteLine($"\n测试激活 key（仅用于当前机器调试）：\n{testKey}\n");

                Console.WriteLine("尝试使用测试 key 激活并延长试用期...");
                // 这里不走 CheckTrialStatus 的交互，直接内部调用验证逻辑
                // 做法：模拟在试用配置上应用该 key
                // 注意：ValidateAndParseActivationKeyImproved 是内部方法，这里通过 CheckTrialStatus 现有流程来测试：
                Console.WriteLine("请在试用到期后，按提示输入上面的测试 key，验证授权流程是否正常。\n");

                return Task.CompletedTask;
            }
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