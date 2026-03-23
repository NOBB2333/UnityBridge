using Microsoft.Extensions.Configuration;
using SqlSugar;

namespace UnityBridge.Crawler;

/// <summary>
/// 爬虫主程序入口。
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("   UnityBridge.Crawler - 社交媒体爬虫工具");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        var registry = CrawlerCommandRegistry.Create(typeof(Program).Assembly);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Configuration/appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var options = new CrawlerOptions();
        configuration.GetSection("Crawler").Bind(options);

        Console.WriteLine($"[配置] 签名服务: {options.SignServerUrl}");
        Console.WriteLine($"[配置] 延迟范围: {options.DefaultDelay.MinMs}-{options.DefaultDelay.MaxMs}ms");
        Console.WriteLine($"[配置] 最大页数: {options.MaxPages}");
        Console.WriteLine();

        var db = CrawlerStorageHelper.CreateDb(options.Database);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n[中断] 正在取消任务...");
        };

        if (args.Length == 0)
        {
            ShowHelp(registry);
            return;
        }

        var inputAction = args[0];
        if (string.Equals(inputAction, "help", StringComparison.OrdinalIgnoreCase))
        {
            ShowHelp(registry);
            return;
        }

        if (!registry.TryResolveAction(inputAction, out var action))
        {
            Console.WriteLine($"[错误] 未知命令: {inputAction}");
            ShowHelp(registry);
            return;
        }

        var parsed = ParseArgs(args.Skip(1).ToArray());

        try
        {
            var platform = ResolvePlatform(action, parsed, registry);
            await DispatchAsync(registry, action, parsed, options, db, platform, cts.Token);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[中断] 任务已取消。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 任务执行失败: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("[完成] 程序执行结束。");
    }

    private static async Task DispatchAsync(
        CrawlerCommandRegistry registry,
        RegisteredActionDefinition action,
        ParsedArgs parsed,
        CrawlerOptions options,
        SqlSugarClient db,
        string platform,
        CancellationToken ct)
    {
        var baseContext = new CrawlerCommandContext(action.Name, parsed, options, db, ct);

        if (string.Equals(platform, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (!action.SupportsAllPlatforms)
            {
                throw new InvalidOperationException($"[错误] 命令 {action.Name} 不支持平台 all。");
            }

            var platformActions = registry.GetActionsForAllPlatforms(action.Name);
            if (platformActions.Count == 0)
            {
                Console.WriteLine($"[警告] 命令 {action.Name} 没有可执行的平台实现。");
                return;
            }

            if (action.RunInParallelForAll)
            {
                var tasks = platformActions
                    .Select(platformAction =>
                    {
                        var context = baseContext.WithPlatform(platformAction.Platform, platformAction.PlatformDisplayName);
                        return platformAction.ExecuteAsync(context);
                    })
                    .ToList();

                await Task.WhenAll(tasks);
                return;
            }

            foreach (var platformAction in platformActions)
            {
                var context = baseContext.WithPlatform(platformAction.Platform, platformAction.PlatformDisplayName);
                await platformAction.ExecuteAsync(context);
            }

            return;
        }

        if (!registry.TryGetActionForPlatform(action.Name, platform, out var registeredAction))
        {
            throw new InvalidOperationException($"[错误] 命令 {action.Name} 不支持平台: {platform}");
        }

        await registeredAction.ExecuteAsync(baseContext.WithPlatform(registeredAction.Platform, registeredAction.PlatformDisplayName));
    }

    private static string ResolvePlatform(
        RegisteredActionDefinition action,
        ParsedArgs parsed,
        CrawlerCommandRegistry registry)
    {
        if (action.PlatformArgumentIndex < 0)
        {
            return "all";
        }

        if (parsed.Positionals.Count <= action.PlatformArgumentIndex)
        {
            if (action.PlatformOptional)
            {
                return "all";
            }

            throw new InvalidOperationException($"[错误] 命令 {action.Name} 缺少平台参数。");
        }

        var rawPlatform = parsed.Positionals[action.PlatformArgumentIndex];
        if (string.IsNullOrWhiteSpace(rawPlatform))
        {
            if (action.PlatformOptional)
            {
                return "all";
            }

            throw new InvalidOperationException($"[错误] 命令 {action.Name} 缺少平台参数。");
        }

        return registry.NormalizePlatform(rawPlatform);
    }

    internal static ParsedArgs ParseArgs(string[] args)
    {
        var positionals = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                positionals.Add(token);
                continue;
            }

            var optionToken = token[2..];
            var equalIdx = optionToken.IndexOf('=');
            if (equalIdx >= 0)
            {
                var key = optionToken[..equalIdx];
                var value = optionToken[(equalIdx + 1)..];
                options[key] = value;
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[optionToken] = args[i + 1];
                i++;
            }
            else
            {
                options[optionToken] = "true";
            }
        }

        return new ParsedArgs(positionals, options);
    }

    internal static string NormalizeZhihuDetailType(string? detailType)
    {
        return detailType?.Trim().ToLowerInvariant() switch
        {
            "answer" or "answers" => "answer",
            "article" or "articles" => "article",
            "video" or "zvideo" or "videos" => "video",
            _ => "article"
        };
    }

    internal static string NormalizeZhihuContentType(string? contentType)
    {
        return contentType?.Trim().ToLowerInvariant() switch
        {
            "article" or "articles" => "article",
            "zvideo" or "video" or "videos" => "zvideo",
            _ => "answer"
        };
    }

    internal static string NormalizeZhihuCreatorType(string? creatorType)
    {
        return creatorType?.Trim().ToLowerInvariant() switch
        {
            "articles" or "article" => "articles",
            "videos" or "video" or "zvideo" => "videos",
            _ => "answers"
        };
    }

    private static void ShowHelp(CrawlerCommandRegistry registry)
    {
        Console.WriteLine("用法:");
        Console.WriteLine("  UnityBridge.Crawler search <关键词> [平台] [--max-pages 10]");
        Console.WriteLine("  UnityBridge.Crawler detail <平台> <内容ID> [参数]");
        Console.WriteLine("  UnityBridge.Crawler comments <平台> <内容ID> [参数]");
        Console.WriteLine("  UnityBridge.Crawler creator <平台> <创作者ID> [参数]");
        Console.WriteLine("  UnityBridge.Crawler homefeed [平台] [--count 12]");
        Console.WriteLine("  UnityBridge.Crawler login <平台> [--method qr] [--write-config true]");
        Console.WriteLine("  UnityBridge.Crawler login-check [平台]");
        Console.WriteLine();
        Console.WriteLine("平台选项:");
        Console.WriteLine("  all      - 所有已配置的平台（默认）");
        foreach (var platform in registry.Platforms.OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  {platform.Name,-8} - {platform.DisplayName}");
        }

        Console.WriteLine();
        Console.WriteLine("常用参数示例:");
        Console.WriteLine("  detail xhs <noteId> --xsec-token <token>");
        Console.WriteLine("  detail zhihu <id> --type answer --question-id <qid>");
        Console.WriteLine("  comments tieba <postId> --page 1 --parent-comment-id <pid> --tieba-id <fid>");
        Console.WriteLine("  creator weibo <uid> --container-id <containerId> --max-pages 2");
        Console.WriteLine("  login bili --method qr --write-config true");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  UnityBridge.Crawler search \"人工智能\"");
        Console.WriteLine("  UnityBridge.Crawler search \"Python教程\" bili");
        Console.WriteLine("  UnityBridge.Crawler login bili");
        Console.WriteLine("  UnityBridge.Crawler detail douyin 7495763494292188425");
        Console.WriteLine("  UnityBridge.Crawler comments bili 114514 --max-pages 5 --include-sub true");
        Console.WriteLine("  UnityBridge.Crawler creator xhs 5f6a8a0f0000000001001234 --max-pages 3");
        Console.WriteLine("  UnityBridge.Crawler login-check all");
        Console.WriteLine();
        Console.WriteLine("配置文件: Configuration/appsettings.json");
    }
}
