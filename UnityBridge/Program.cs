namespace UnityBridge;

using Flurl.Http;
using UnityBridge.Api.Dify;
using UnityBridge.Api.Sino;
using UnityBridge.Commands;
using UnityBridge.Helpers;
using UnityBridge.Options;

class Program
{
    static async Task Main(string[] args)
    {
        // 试用期检测 - 必须在程序启动时最先执行
        if (!TrialManager.CheckTrialStatus())
        {
            return; // 试用期已过期且未激活，退出程序
        }

        // 从配置文件加载下载相关配置（优先使用 DifyMigration.Host）
        var migration = ConfigManager.DifyMigration;
        var dlSection = migration.Host;

        if (dlSection is null || string.IsNullOrWhiteSpace(dlSection.BaseUrl))
        {
            Console.Error.WriteLine(
                "配置文件中未找到有效的 Host / Download.BaseUrl，请在 Configuration/DifyMigration.json 中配置 Host.BaseUrl");
            return;
        }

        Dictionary<string, string> headers;
        if (dlSection.Headers is { Count: > 0 })
            headers = new Dictionary<string, string>(dlSection.Headers, StringComparer.OrdinalIgnoreCase);
        else
            headers = HeaderHelper.ParseHeaders(dlSection.HeaderSpec ?? string.Empty);


        var options = new DifyApiClientOptions { Endpoint = dlSection.BaseUrl, Timeout = 60000 }; // 60 seconds

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

        // WoWeb (Sion) 客户端初始化（可选）
        CompanyApiClient? woWebClient = null;
        Dictionary<string, string>? sionHeaders = null;
        var sionSection = ConfigManager.SionWebApp?.Host;
        if (sionSection is not null && !string.IsNullOrWhiteSpace(sionSection.BaseUrl))
        {
            var sionClientOptions = new CompanyApiClientOptions
            {
                Endpoint = sionSection.BaseUrl,
                Timeout = 60000,
                Token = sionSection.Token,
                OverToken = sionSection.OverToken,
                TenantId = sionSection.TenantId,
                Origin = sionSection.Origin,
                Referer = sionSection.Referer,
                AcceptLanguage = sionSection.AcceptLanguage,
                UserAgent = sionSection.UserAgent
            };

            woWebClient = new CompanyApiClient(sionClientOptions);

            if (sionSection.Headers is { Count: > 0 })
            {
                sionHeaders = new Dictionary<string, string>(sionSection.Headers, StringComparer.OrdinalIgnoreCase);
                foreach (var header in sionHeaders)
                {
                    woWebClient.FlurlClient.WithHeader(header.Key, header.Value);
                }
            }
        }

        // 菜单选项定义
        var menuOptions = new[]
        {
            "1) 下载 (导出) 应用",
            "2) 上传 (导入) 应用",
            "3) 检测文件/文件夹编码",
            "4) 发布 / 管理 API Key",
            "5) 获取应用详情 / 导出综合表",
            "6) 查看试用期信息",
            "7) 测试本机授权 (显示机器码并生成测试激活 key)",
            "8) 一次性测试 WoWeb 项目流程",
            "9) 批量发布所有工作流并创建工具",
            "10) 分析 Chat 应用与工作流关系图",
            "11) 打包 Chat 应用及其所需工作流"
        };

        // 菜单选项与命令方法的映射关系
        // 选项 1 -> DifyMigrationCommand.ExportAsync
        // 选项 2 -> DifyMigrationCommand.ImportAsync
        // 选项 3 -> DetectEncodingCommand.RunAsync
        // 选项 4 -> DifyAppCommand.ManageApiKeysAsync
        // 选项 5 -> DifyAppCommand.InspectAppsAsync
        // 选项 6 -> TrialManager.ShowTrialInfo
        // 选项 7 -> 本机授权测试 (打印机器码 + 生成并应用测试激活 key)
        // 选项 8 -> RunWoWebSmokeTestsAsync
        // 选项 9 -> DifyMigrationCommand.PublishAllWorkflowsAsync
        var commands = new Dictionary<string, Func<Task>>
        {
            ["1"] = async () => await DifyMigrationCommand.ExportAsync(client), // 下载 (导出) 应用
            ["2"] = async () => await DifyMigrationCommand.ImportAsync(client), // 上传 (导入) 应用
            ["3"] = async () => await DetectEncodingCommand.RunAsync(), // 检测文件/文件夹编码
            ["4"] = async () => await DifyAppCommand.ManageApiKeysAsync(client), // 发布/管理 API Key
            ["5"] = async () => await DifyAppCommand.InspectAppsAsync(client), // 获取应用详情 / 导出综合表
            ["6"] = () =>
            {
                TrialManager.ShowTrialInfo();
                return Task.CompletedTask;
            }, // 查看试用期信息
            ["7"] = () => // 本机授权测试
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
            },
            ["8"] = async () => await RunWoWebSmokeTestsAsync(woWebClient),
            ["9"] = async () => await DifyMigrationCommand.PublishAllWorkflowsAsync(client), // 批量发布所有工作流并创建工具
            ["10"] = async () => await DifyMigrationCommand.AnalyzeChatWorkflowRelationsAsync(), // 分析 Chat 应用与工作流关系图
            ["11"] = async () => await DifyMigrationCommand.PrepareChatAppPackagesAsync() // 打包 Chat 应用及依赖工作流
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

    private static async Task RunWoWebSmokeTestsAsync(CompanyApiClient? woWebClient)
    {
        if (woWebClient is null)
        {
            Console.WriteLine(
                "未检测到可用的 SionWebApp 配置，无法执行 WoWeb 流程测试。请在 Configuration/SionWebApp.json 中填写 Host.BaseUrl 和 Headers。");
            return;
        }

        var testFlows = new List<Func<CompanyApiClient, Task>>
        {
            client => SinoKnowledgeCommand.ListIndividualLibrariesAsync(client),
            client => SinoKnowledgeCommand.ShowLibraryOverviewAsync(client)
        };

        Console.WriteLine("\n开始执行 WoWeb 流程测试流水线...");
        for (var i = 0; i < testFlows.Count; i++)
        {
            var flow = testFlows[i];
            var stepName = flow.Method.Name;
            Console.WriteLine($"[{i + 1}/{testFlows.Count}] {stepName}");

            try
            {
                await flow(woWebClient);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"步骤 \"{stepName}\" 失败: {ex.Message}");
            }
        }

        Console.WriteLine("WoWeb 流程测试执行完毕。\n");
    }
}