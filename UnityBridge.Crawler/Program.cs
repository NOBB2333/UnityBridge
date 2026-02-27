using Microsoft.Extensions.Configuration;
using QRCoder;
using SqlSugar;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        // 加载配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Configuration/appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var options = new CrawlerOptions();
        configuration.GetSection("Crawler").Bind(options);

        Console.WriteLine($"[配置] 签名服务: {options.SignServerUrl}");
        Console.WriteLine($"[配置] 数据库类型: {options.Database.Type}");
        Console.WriteLine($"[配置] 延迟范围: {options.DefaultDelay.MinMs}-{options.DefaultDelay.MaxMs}ms");
        Console.WriteLine($"[配置] 最大页数: {options.MaxPages}");
        Console.WriteLine();

        // 初始化数据库
        var db = options.Database.Type.ToLowerInvariant() switch
        {
            "mysql" => CrawlerStorageHelper.CreateMySqlDb(options.Database.ConnectionString),
            _ => CrawlerStorageHelper.CreateSqliteDb(options.Database.ConnectionString.Replace("Data Source=", "").Replace(";", ""))
        };

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n[中断] 正在取消任务...");
        };

        // 解析命令行参数
        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var command = args[0].ToLowerInvariant();
        var parsed = ParseArgs(args.Skip(1).ToArray());

        switch (command)
        {
            case "search":
                if (parsed.Positionals.Count == 0)
                {
                    Console.WriteLine("[错误] 请提供搜索关键词！");
                    ShowHelp();
                    return;
                }

                await SearchAsync(
                    db,
                    options,
                    parsed.Positionals[0],
                    NormalizePlatform(parsed.Positionals.Count > 1 ? parsed.Positionals[1] : "all"),
                    GetIntOption(parsed.Options, "max-pages", options.MaxPages),
                    cts.Token);
                break;

            case "detail":
                await HandleDetailCommandAsync(db, options, parsed, cts.Token);
                break;

            case "comments":
                await HandleCommentsCommandAsync(db, options, parsed, cts.Token);
                break;

            case "creator":
                await HandleCreatorCommandAsync(db, options, parsed, cts.Token);
                break;

            case "homefeed":
                await HandleHomeFeedCommandAsync(db, options, parsed, cts.Token);
                break;

            case "login-check":
                await HandleLoginCheckCommandAsync(options, parsed, cts.Token);
                break;

            case "login":
                await HandleLoginCommandAsync(options, parsed, cts.Token);
                break;

            case "help":
                ShowHelp();
                break;

            default:
                Console.WriteLine($"[错误] 未知命令: {command}");
                ShowHelp();
                break;
        }

        Console.WriteLine();
        Console.WriteLine("[完成] 程序执行结束。");
    }

    private static async Task SearchAsync(
        SqlSugarClient db,
        CrawlerOptions options,
        string keyword,
        string platform,
        int maxPages,
        CancellationToken ct)
    {
        Console.WriteLine($"[搜索] 关键词: {keyword}");
        Console.WriteLine($"[搜索] 平台: {platform}");
        Console.WriteLine($"[搜索] 最大页数: {maxPages}");
        Console.WriteLine();

        var tasks = new List<Task>();

        // 小红书
        if ((platform == "all" || platform == "xhs") && options.Platforms.XiaoHongShu.Enabled)
        {
            if (!string.IsNullOrEmpty(options.Platforms.XiaoHongShu.Cookies))
            {
                var client = CrawlerFactory.CreateXhsClient(
                    options.Platforms.XiaoHongShu.Cookies,
                    options.SignServerUrl);
                tasks.Add(CrawlerCommand.XhsSearchAsync(client, db, keyword,
                    maxPages, options.DefaultDelay.MinMs, options.DefaultDelay.MaxMs, ct));
            }
            else
            {
                Console.WriteLine("[跳过] 小红书: 未配置 Cookies");
            }
        }

        // B站
        if ((platform == "all" || platform == "bili") && options.Platforms.BiliBili.Enabled)
        {
            if (!string.IsNullOrEmpty(options.Platforms.BiliBili.Cookies))
            {
                var client = CrawlerFactory.CreateBiliClient(
                    options.Platforms.BiliBili.Cookies,
                    options.SignServerUrl);
                tasks.Add(CrawlerCommand.BiliSearchAsync(client, db, keyword,
                    maxPages, options.DefaultDelay.MinMs, options.DefaultDelay.MaxMs, ct));
            }
            else
            {
                Console.WriteLine("[跳过] B站: 未配置 Cookies");
            }
        }

        // 抖音
        if ((platform == "all" || platform == "douyin") && options.Platforms.Douyin.Enabled)
        {
            if (!string.IsNullOrEmpty(options.Platforms.Douyin.Cookies))
            {
                var client = CrawlerFactory.CreateDouyinClient(
                    options.Platforms.Douyin.Cookies,
                    options.SignServerUrl);
                tasks.Add(CrawlerCommand.DouyinSearchAsync(client, db, keyword,
                    maxPages, options.DefaultDelay.MinMs, options.DefaultDelay.MaxMs, ct));
            }
            else
            {
                Console.WriteLine("[跳过] 抖音: 未配置 Cookies");
            }
        }

        // 贴吧
        if ((platform == "all" || platform == "tieba") && options.Platforms.Tieba.Enabled)
        {
            if (!string.IsNullOrEmpty(options.Platforms.Tieba.Cookies))
            {
                var client = CrawlerFactory.CreateTiebaClient(options.Platforms.Tieba.Cookies);
                tasks.Add(CrawlerCommand.TiebaSearchAsync(client, keyword, maxPages, ct));
            }
            else
            {
                Console.WriteLine("[跳过] 贴吧: 未配置 Cookies");
            }
        }

        // 快手
        if ((platform == "all" || platform == "kuaishou") && options.Platforms.Kuaishou.Enabled)
        {
            if (!string.IsNullOrEmpty(options.Platforms.Kuaishou.Cookies))
            {
                var client = CrawlerFactory.CreateKuaishouClient(options.Platforms.Kuaishou.Cookies);
                tasks.Add(CrawlerCommand.KuaishouSearchAsync(client, db, keyword,
                    maxPages, options.DefaultDelay.MinMs, options.DefaultDelay.MaxMs, ct));
            }
            else
            {
                Console.WriteLine("[跳过] 快手: 未配置 Cookies");
            }
        }

        // 知乎
        if ((platform == "all" || platform == "zhihu") && options.Platforms.Zhihu.Enabled)
        {
            if (!string.IsNullOrEmpty(options.Platforms.Zhihu.Cookies))
            {
                var client = CrawlerFactory.CreateZhihuClient(
                    options.Platforms.Zhihu.Cookies,
                    options.SignServerUrl);
                tasks.Add(CrawlerCommand.ZhihuSearchAsync(client, db, keyword,
                    maxPages, options.DefaultDelay.MinMs, options.DefaultDelay.MaxMs, ct));
            }
            else
            {
                Console.WriteLine("[跳过] 知乎: 未配置 Cookies");
            }
        }

        // 微博
        if ((platform == "all" || platform == "weibo") && options.Platforms.Weibo.Enabled)
        {
            if (!string.IsNullOrEmpty(options.Platforms.Weibo.Cookies))
            {
                var client = CrawlerFactory.CreateWeiboClient(options.Platforms.Weibo.Cookies);
                tasks.Add(CrawlerCommand.WeiboSearchAsync(client, db, keyword,
                    maxPages, options.DefaultDelay.MinMs, options.DefaultDelay.MaxMs, ct));
            }
            else
            {
                Console.WriteLine("[跳过] 微博: 未配置 Cookies");
            }
        }

        if (tasks.Count == 0)
        {
            Console.WriteLine("[警告] 没有可执行的任务，请检查配置文件中的 Cookies。");
            return;
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[中断] 所有任务已取消。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 任务执行失败: {ex.Message}");
        }
    }

    private static async Task HandleDetailCommandAsync(
        SqlSugarClient db,
        CrawlerOptions options,
        ParsedArgs args,
        CancellationToken ct)
    {
        if (args.Positionals.Count < 2)
        {
            Console.WriteLine("[错误] 用法: detail <平台> <内容ID> [参数]");
            return;
        }

        var platform = NormalizePlatform(args.Positionals[0]);
        var contentId = args.Positionals[1];

        switch (platform)
        {
            case "xhs":
                if (!EnsureReady("小红书", options.Platforms.XiaoHongShu)) return;
                {
                    var client = CrawlerFactory.CreateXhsClient(options.Platforms.XiaoHongShu.Cookies, options.SignServerUrl);
                    var xsecToken = GetOption(args.Options, "xsec-token", "xsec_token");
                    await CrawlerCommand.XhsGetNoteDetailAsync(client, db, contentId, xsecToken, ct);
                }
                break;

            case "bili":
                if (!EnsureReady("B站", options.Platforms.BiliBili)) return;
                {
                    var client = CrawlerFactory.CreateBiliClient(options.Platforms.BiliBili.Cookies, options.SignServerUrl);
                    var aid = GetOption(args.Options, "aid");
                    var bvid = GetOption(args.Options, "bvid");
                    if (string.IsNullOrEmpty(aid) && string.IsNullOrEmpty(bvid))
                    {
                        if (contentId.StartsWith("BV", StringComparison.OrdinalIgnoreCase))
                            bvid = contentId;
                        else
                            aid = contentId;
                    }
                    await CrawlerCommand.BiliGetVideoDetailAsync(client, db, aid, bvid, ct);
                }
                break;

            case "douyin":
                if (!EnsureReady("抖音", options.Platforms.Douyin)) return;
                {
                    var client = CrawlerFactory.CreateDouyinClient(options.Platforms.Douyin.Cookies, options.SignServerUrl);
                    await CrawlerCommand.DouyinGetAwemeDetailAsync(client, db, contentId, ct);
                }
                break;

            case "kuaishou":
                if (!EnsureReady("快手", options.Platforms.Kuaishou)) return;
                {
                    var client = CrawlerFactory.CreateKuaishouClient(options.Platforms.Kuaishou.Cookies);
                    await CrawlerCommand.KuaishouGetVideoDetailAsync(client, db, contentId, ct);
                }
                break;

            case "zhihu":
                if (!EnsureReady("知乎", options.Platforms.Zhihu)) return;
                {
                    var client = CrawlerFactory.CreateZhihuClient(options.Platforms.Zhihu.Cookies, options.SignServerUrl);
                    var detailType = NormalizeZhihuDetailType(GetOption(args.Options, "type", "content-type", "content_type"));
                    var questionId = GetOption(args.Options, "question-id", "question_id");
                    await CrawlerCommand.ZhihuGetContentDetailHtmlAsync(client, contentId, detailType, questionId, ct);
                }
                break;

            case "weibo":
                if (!EnsureReady("微博", options.Platforms.Weibo)) return;
                {
                    var client = CrawlerFactory.CreateWeiboClient(options.Platforms.Weibo.Cookies);
                    await CrawlerCommand.WeiboGetNoteDetailHtmlAsync(client, contentId, ct);
                }
                break;

            case "tieba":
                if (!EnsureReady("贴吧", options.Platforms.Tieba)) return;
                {
                    var client = CrawlerFactory.CreateTiebaClient(options.Platforms.Tieba.Cookies);
                    await CrawlerCommand.TiebaGetPostDetailHtmlAsync(client, contentId, ct);
                }
                break;

            default:
                Console.WriteLine($"[错误] 不支持的平台: {platform}");
                break;
        }
    }

    private static async Task HandleCommentsCommandAsync(
        SqlSugarClient db,
        CrawlerOptions options,
        ParsedArgs args,
        CancellationToken ct)
    {
        if (args.Positionals.Count < 2)
        {
            Console.WriteLine("[错误] 用法: comments <平台> <内容ID> [参数]");
            return;
        }

        var platform = NormalizePlatform(args.Positionals[0]);
        var contentId = args.Positionals[1];
        var maxPages = GetIntOption(args.Options, "max-pages", 3);
        var includeSub = GetBoolOption(args.Options, "include-sub", true);

        switch (platform)
        {
            case "xhs":
                if (!EnsureReady("小红书", options.Platforms.XiaoHongShu)) return;
                {
                    var client = CrawlerFactory.CreateXhsClient(options.Platforms.XiaoHongShu.Cookies, options.SignServerUrl);
                    var xsecToken = GetOption(args.Options, "xsec-token", "xsec_token");
                    await CrawlerCommand.XhsGetCommentsAsync(client, db, contentId, xsecToken, includeSub, ct);
                }
                break;

            case "bili":
                if (!EnsureReady("B站", options.Platforms.BiliBili)) return;
                {
                    var client = CrawlerFactory.CreateBiliClient(options.Platforms.BiliBili.Cookies, options.SignServerUrl);
                    await CrawlerCommand.BiliGetCommentsAsync(client, db, contentId, maxPages, includeSub, ct);
                }
                break;

            case "douyin":
                if (!EnsureReady("抖音", options.Platforms.Douyin)) return;
                {
                    var client = CrawlerFactory.CreateDouyinClient(options.Platforms.Douyin.Cookies, options.SignServerUrl);
                    await CrawlerCommand.DouyinGetCommentsAsync(client, db, contentId, maxPages, includeSub, ct);
                }
                break;

            case "kuaishou":
                if (!EnsureReady("快手", options.Platforms.Kuaishou)) return;
                {
                    var client = CrawlerFactory.CreateKuaishouClient(options.Platforms.Kuaishou.Cookies);
                    await CrawlerCommand.KuaishouGetCommentsAsync(client, db, contentId, maxPages, includeSub, ct);
                }
                break;

            case "zhihu":
                if (!EnsureReady("知乎", options.Platforms.Zhihu)) return;
                {
                    var client = CrawlerFactory.CreateZhihuClient(options.Platforms.Zhihu.Cookies, options.SignServerUrl);
                    var contentType = NormalizeZhihuContentType(GetOption(args.Options, "content-type", "content_type", "type"));
                    await CrawlerCommand.ZhihuGetCommentsAsync(client, db, contentId, contentType, maxPages, includeSub, ct);
                }
                break;

            case "weibo":
                if (!EnsureReady("微博", options.Platforms.Weibo)) return;
                {
                    var client = CrawlerFactory.CreateWeiboClient(options.Platforms.Weibo.Cookies);
                    await CrawlerCommand.WeiboGetCommentsAsync(client, db, contentId, maxPages, ct);
                }
                break;

            case "tieba":
                if (!EnsureReady("贴吧", options.Platforms.Tieba)) return;
                {
                    var client = CrawlerFactory.CreateTiebaClient(options.Platforms.Tieba.Cookies);
                    var page = GetIntOption(args.Options, "page", 1);
                    var parentCommentId = GetOption(args.Options, "parent-comment-id", "parent_comment_id", "pid");
                    var tiebaId = GetOption(args.Options, "tieba-id", "tieba_id", "fid");
                    await CrawlerCommand.TiebaGetCommentsHtmlAsync(client, contentId, page, parentCommentId, tiebaId, ct);
                }
                break;

            default:
                Console.WriteLine($"[错误] 不支持的平台: {platform}");
                break;
        }
    }

    private static async Task HandleCreatorCommandAsync(
        SqlSugarClient db,
        CrawlerOptions options,
        ParsedArgs args,
        CancellationToken ct)
    {
        if (args.Positionals.Count < 2)
        {
            Console.WriteLine("[错误] 用法: creator <平台> <创作者ID> [参数]");
            return;
        }

        var platform = NormalizePlatform(args.Positionals[0]);
        var creatorId = args.Positionals[1];
        var maxPages = GetIntOption(args.Options, "max-pages", 2);

        switch (platform)
        {
            case "xhs":
                if (!EnsureReady("小红书", options.Platforms.XiaoHongShu)) return;
                {
                    var client = CrawlerFactory.CreateXhsClient(options.Platforms.XiaoHongShu.Cookies, options.SignServerUrl);
                    var xsecToken = GetOption(args.Options, "xsec-token", "xsec_token");
                    await CrawlerCommand.XhsGetCreatorNotesAsync(client, db, creatorId, maxPages, xsecToken, ct);
                }
                break;

            case "bili":
                if (!EnsureReady("B站", options.Platforms.BiliBili)) return;
                {
                    var client = CrawlerFactory.CreateBiliClient(options.Platforms.BiliBili.Cookies, options.SignServerUrl);
                    await CrawlerCommand.BiliGetCreatorAsync(client, db, creatorId, maxPages, ct);
                }
                break;

            case "douyin":
                if (!EnsureReady("抖音", options.Platforms.Douyin)) return;
                {
                    var client = CrawlerFactory.CreateDouyinClient(options.Platforms.Douyin.Cookies, options.SignServerUrl);
                    await CrawlerCommand.DouyinGetCreatorAsync(client, db, creatorId, maxPages, ct);
                }
                break;

            case "kuaishou":
                if (!EnsureReady("快手", options.Platforms.Kuaishou)) return;
                {
                    var client = CrawlerFactory.CreateKuaishouClient(options.Platforms.Kuaishou.Cookies);
                    await CrawlerCommand.KuaishouGetCreatorAsync(client, db, creatorId, maxPages, ct);
                }
                break;

            case "zhihu":
                if (!EnsureReady("知乎", options.Platforms.Zhihu)) return;
                {
                    var client = CrawlerFactory.CreateZhihuClient(options.Platforms.Zhihu.Cookies, options.SignServerUrl);
                    var creatorType = NormalizeZhihuCreatorType(GetOption(args.Options, "type", "content-type", "content_type"));
                    await CrawlerCommand.ZhihuGetCreatorContentsAsync(client, db, creatorId, creatorType, maxPages, ct);
                }
                break;

            case "weibo":
                if (!EnsureReady("微博", options.Platforms.Weibo)) return;
                {
                    var client = CrawlerFactory.CreateWeiboClient(options.Platforms.Weibo.Cookies);
                    var containerId = GetOption(args.Options, "container-id", "container_id");
                    await CrawlerCommand.WeiboGetCreatorAsync(client, db, creatorId, maxPages, containerId, ct);
                }
                break;

            case "tieba":
                if (!EnsureReady("贴吧", options.Platforms.Tieba)) return;
                {
                    var client = CrawlerFactory.CreateTiebaClient(options.Platforms.Tieba.Cookies);
                    await CrawlerCommand.TiebaGetCreatorPostsAsync(client, db, creatorId, maxPages, ct);
                }
                break;

            default:
                Console.WriteLine($"[错误] 不支持的平台: {platform}");
                break;
        }
    }

    private static async Task HandleHomeFeedCommandAsync(
        SqlSugarClient db,
        CrawlerOptions options,
        ParsedArgs args,
        CancellationToken ct)
    {
        var platform = NormalizePlatform(args.Positionals.Count > 0 ? args.Positionals[0] : "all");
        var count = GetIntOption(args.Options, "count", 12);

        if ((platform == "all" || platform == "xhs") && EnsureReadyOrSkip("小红书", options.Platforms.XiaoHongShu))
        {
            var client = CrawlerFactory.CreateXhsClient(options.Platforms.XiaoHongShu.Cookies, options.SignServerUrl);
            await CrawlerCommand.XhsGetHomeFeedAsync(client, db, count, ct);
        }

        if ((platform == "all" || platform == "bili") && EnsureReadyOrSkip("B站", options.Platforms.BiliBili))
        {
            var client = CrawlerFactory.CreateBiliClient(options.Platforms.BiliBili.Cookies, options.SignServerUrl);
            await CrawlerCommand.BiliGetHomeFeedAsync(client, db, count, ct);
        }

        if ((platform == "all" || platform == "douyin") && EnsureReadyOrSkip("抖音", options.Platforms.Douyin))
        {
            var client = CrawlerFactory.CreateDouyinClient(options.Platforms.Douyin.Cookies, options.SignServerUrl);
            await CrawlerCommand.DouyinGetHomeFeedAsync(client, db, count, ct);
        }

        if ((platform == "all" || platform == "kuaishou") && EnsureReadyOrSkip("快手", options.Platforms.Kuaishou))
        {
            var client = CrawlerFactory.CreateKuaishouClient(options.Platforms.Kuaishou.Cookies);
            await CrawlerCommand.KuaishouGetHomeFeedAsync(client, db, count, ct);
        }

        if (platform == "tieba" || platform == "all")
        {
            if (EnsureReadyOrSkip("贴吧", options.Platforms.Tieba))
            {
                var client = CrawlerFactory.CreateTiebaClient(options.Platforms.Tieba.Cookies);
                var tiebaName = GetOption(args.Options, "tieba-name", "tieba_name") ?? "贴吧";
                var pageNum = GetIntOption(args.Options, "page", 0);
                await CrawlerCommand.TiebaGetForumHtmlAsync(client, tiebaName, pageNum, ct);
            }
        }

        if (platform == "zhihu")
        {
            Console.WriteLine("[提示] 知乎暂无统一 homefeed API，请用 search/creator 代替。");
        }

        if (platform == "weibo")
        {
            Console.WriteLine("[提示] 微博暂无统一 homefeed API，请用 search/creator 代替。");
        }
    }

    private static async Task HandleLoginCheckCommandAsync(
        CrawlerOptions options,
        ParsedArgs args,
        CancellationToken ct)
    {
        var platform = NormalizePlatform(args.Positionals.Count > 0 ? args.Positionals[0] : "all");

        async Task CheckAsync(string name, Func<Task<bool>> action)
        {
            try
            {
                var ok = await action();
                Console.WriteLine($"[Login] {name}: {(ok ? "OK" : "FAIL")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Login] {name}: FAIL ({ex.Message})");
            }
        }

        if ((platform == "all" || platform == "xhs") && EnsureReadyOrSkip("小红书", options.Platforms.XiaoHongShu))
        {
            var client = CrawlerFactory.CreateXhsClient(options.Platforms.XiaoHongShu.Cookies, options.SignServerUrl);
            await CheckAsync("小红书", () => CrawlerCommand.XhsLoginCheckAsync(client, ct));
        }

        if ((platform == "all" || platform == "bili") && EnsureReadyOrSkip("B站", options.Platforms.BiliBili))
        {
            var client = CrawlerFactory.CreateBiliClient(options.Platforms.BiliBili.Cookies, options.SignServerUrl);
            await CheckAsync("B站", () => CrawlerCommand.BiliLoginCheckAsync(client, ct));
        }

        if ((platform == "all" || platform == "douyin") && EnsureReadyOrSkip("抖音", options.Platforms.Douyin))
        {
            var client = CrawlerFactory.CreateDouyinClient(options.Platforms.Douyin.Cookies, options.SignServerUrl);
            await CheckAsync("抖音", () => CrawlerCommand.DouyinLoginCheckAsync(client, ct));
        }

        if ((platform == "all" || platform == "tieba") && EnsureReadyOrSkip("贴吧", options.Platforms.Tieba))
        {
            var client = CrawlerFactory.CreateTiebaClient(options.Platforms.Tieba.Cookies);
            await CheckAsync("贴吧", () => CrawlerCommand.TiebaLoginCheckAsync(client, ct));
        }

        if ((platform == "all" || platform == "kuaishou") && EnsureReadyOrSkip("快手", options.Platforms.Kuaishou))
        {
            var client = CrawlerFactory.CreateKuaishouClient(options.Platforms.Kuaishou.Cookies);
            await CheckAsync("快手", () => CrawlerCommand.KuaishouLoginCheckAsync(client, ct));
        }

        if ((platform == "all" || platform == "zhihu") && EnsureReadyOrSkip("知乎", options.Platforms.Zhihu))
        {
            var client = CrawlerFactory.CreateZhihuClient(options.Platforms.Zhihu.Cookies, options.SignServerUrl);
            await CheckAsync("知乎", () => CrawlerCommand.ZhihuLoginCheckAsync(client, ct));
        }

        if ((platform == "all" || platform == "weibo") && EnsureReadyOrSkip("微博", options.Platforms.Weibo))
        {
            var client = CrawlerFactory.CreateWeiboClient(options.Platforms.Weibo.Cookies);
            await CheckAsync("微博", () => CrawlerCommand.WeiboLoginCheckAsync(client, ct));
        }
    }

    private static async Task HandleLoginCommandAsync(
        CrawlerOptions options,
        ParsedArgs args,
        CancellationToken ct)
    {
        if (args.Positionals.Count == 0)
        {
            Console.WriteLine("[错误] 用法: login <平台> [参数]");
            Console.WriteLine("示例: UnityBridge.Crawler login bili --method qr --write-config true");
            return;
        }

        var platform = NormalizePlatform(args.Positionals[0]);
        var method = (GetOption(args.Options, "method") ?? "qr").Trim().ToLowerInvariant();
        var writeConfig = GetBoolOption(args.Options, "write-config", true);

        switch (platform)
        {
            case "bili":
                if (method != "qr")
                {
                    Console.WriteLine($"[错误] B站当前仅支持 --method qr，收到: {method}");
                    return;
                }

                {
                    var timeout = GetIntOption(args.Options, "timeout", 120);
                    var pollInterval = GetIntOption(args.Options, "poll-interval", 1);
                    var client = CrawlerFactory.CreateBiliClient(options.Platforms.BiliBili.Cookies, options.SignServerUrl);

                    var session = await CrawlerCommand.BiliCreateQrLoginSessionAsync(client, ct);
                    if (session is null)
                    {
                        Console.WriteLine("[Login] 生成二维码失败。");
                        return;
                    }

                    Console.WriteLine("[Login] 请使用哔哩哔哩 APP 扫描下方二维码：");
                    RenderQrCodeToConsole(session.LoginUrl);
                    Console.WriteLine($"[Login] 若终端二维码显示异常，可打开此链接：{session.LoginUrl}");

                    var cookies = await CrawlerCommand.BiliWaitForQrLoginAsync(
                        client, session.QrcodeKey, timeout, pollInterval, ct);

                    if (string.IsNullOrWhiteSpace(cookies))
                    {
                        Console.WriteLine("[Login] 登录失败。");
                        return;
                    }

                    options.Platforms.BiliBili.Cookies = cookies;
                    Console.WriteLine("[Login] 登录成功，B站 Cookies 已写入内存。");

                    if (writeConfig)
                    {
                        if (TryUpdateCookiesInConfig("BiliBili", cookies, out var path, out var error))
                        {
                            Console.WriteLine($"[Login] 已更新配置文件: {path}");
                        }
                        else
                        {
                            Console.WriteLine($"[Login] 配置写入失败: {error}");
                        }
                    }
                }
                break;

            default:
                Console.WriteLine($"[提示] 平台 {platform} 暂未实现自动扫码登录。");
                Console.WriteLine("[提示] 当前可用: login bili --method qr");
                break;
        }
    }

    private static ParsedArgs ParseArgs(string[] args)
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

    private static string? GetOption(Dictionary<string, string> options, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }

    private static int GetIntOption(Dictionary<string, string> options, string key, int fallback)
    {
        var raw = GetOption(options, key);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }

    private static bool GetBoolOption(Dictionary<string, string> options, string key, bool fallback)
    {
        var raw = GetOption(options, key);
        if (string.IsNullOrEmpty(raw))
        {
            return fallback;
        }

        if (bool.TryParse(raw, out var value))
        {
            return value;
        }

        return raw switch
        {
            "1" or "yes" or "on" => true,
            "0" or "no" or "off" => false,
            _ => fallback
        };
    }

    private static bool EnsureReady(string platformName, PlatformConfig config)
    {
        if (!config.Enabled)
        {
            Console.WriteLine($"[错误] {platformName} 未启用。");
            return false;
        }
        if (string.IsNullOrWhiteSpace(config.Cookies))
        {
            Console.WriteLine($"[错误] {platformName} 未配置 Cookies。");
            return false;
        }
        return true;
    }

    private static bool EnsureReadyOrSkip(string platformName, PlatformConfig config)
    {
        if (!config.Enabled)
        {
            Console.WriteLine($"[跳过] {platformName}: 已禁用。");
            return false;
        }
        if (string.IsNullOrWhiteSpace(config.Cookies))
        {
            Console.WriteLine($"[跳过] {platformName}: 未配置 Cookies。");
            return false;
        }
        return true;
    }

    private static string NormalizePlatform(string platform)
    {
        return platform.Trim().ToLowerInvariant() switch
        {
            "" => "all",
            "all" => "all",
            "xhs" or "xiaohongshu" or "xiaohongshu.com" or "小红书" => "xhs",
            "bili" or "bilibili" or "b站" => "bili",
            "douyin" or "dy" or "抖音" => "douyin",
            "tieba" or "tb" or "贴吧" => "tieba",
            "kuaishou" or "ks" or "快手" => "kuaishou",
            "zhihu" or "zh" or "知乎" => "zhihu",
            "weibo" or "wb" or "微博" => "weibo",
            _ => platform.Trim().ToLowerInvariant()
        };
    }

    private static string NormalizeZhihuDetailType(string? detailType)
    {
        return detailType?.Trim().ToLowerInvariant() switch
        {
            "answer" or "answers" => "answer",
            "article" or "articles" => "article",
            "video" or "zvideo" or "videos" => "video",
            _ => "article"
        };
    }

    private static string NormalizeZhihuContentType(string? contentType)
    {
        return contentType?.Trim().ToLowerInvariant() switch
        {
            "article" or "articles" => "article",
            "zvideo" or "video" or "videos" => "zvideo",
            _ => "answer"
        };
    }

    private static string NormalizeZhihuCreatorType(string? creatorType)
    {
        return creatorType?.Trim().ToLowerInvariant() switch
        {
            "articles" or "article" => "articles",
            "videos" or "video" or "zvideo" => "videos",
            _ => "answers"
        };
    }

    private static void RenderQrCodeToConsole(string qrContent)
    {
        var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
        var matrix = BuildQrMatrix(data);
        if (matrix.Count == 0)
        {
            return;
        }

        var consoleWidth = GetConsoleWidthOrDefault();
        var consoleHeight = GetConsoleHeightOrDefault();
        var requiredWidth = matrix.Count;
        var requiredHeight = (matrix.Count + 1) / 2;

        // 始终渲染“无损最小二维码”（compact 模式），不做降采样，避免任何变形。
        // 如果终端太小，先提示再继续打印，让用户自行放大终端或调字体。
        if (requiredWidth > consoleWidth || requiredHeight > consoleHeight)
        {
            Console.WriteLine($"[Login] 当前终端可能过小，建议至少 {requiredWidth}x{requiredHeight}（宽x高）字符窗口。");
        }

        RenderCompactQr(matrix);
    }

    private static List<List<bool>> BuildQrMatrix(QRCodeData data)
    {
        var raw = new List<List<bool>>(data.ModuleMatrix.Count);
        for (var y = 0; y < data.ModuleMatrix.Count; y++)
        {
            var row = new List<bool>(data.ModuleMatrix[y].Count);
            for (var x = 0; x < data.ModuleMatrix[y].Count; x++)
            {
                row.Add(data.ModuleMatrix[y][x] == true);
            }
            raw.Add(row);
        }

        return TrimQuietZone(raw, padding: 1);
    }

    private static List<List<bool>> TrimQuietZone(List<List<bool>> matrix, int padding)
    {
        var size = matrix.Count;
        var minY = size;
        var maxY = -1;
        var minX = size;
        var maxX = -1;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < matrix[y].Count; x++)
            {
                if (!matrix[y][x]) continue;

                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }

        if (maxY < 0 || maxX < 0)
        {
            return matrix;
        }

        minY = Math.Max(0, minY - padding);
        minX = Math.Max(0, minX - padding);
        maxY = Math.Min(size - 1, maxY + padding);
        maxX = Math.Min(size - 1, maxX + padding);

        var result = new List<List<bool>>(maxY - minY + 1);
        for (var y = minY; y <= maxY; y++)
        {
            var row = new List<bool>(maxX - minX + 1);
            for (var x = minX; x <= maxX; x++)
            {
                row.Add(matrix[y][x]);
            }
            result.Add(row);
        }

        return result;
    }

    private static void RenderCompactQr(List<List<bool>> matrix)
    {
        for (var y = 0; y < matrix.Count; y += 2)
        {
            var sb = new StringBuilder(matrix.Count);
            var hasNext = y + 1 < matrix.Count;

            for (var x = 0; x < matrix[y].Count; x++)
            {
                var top = matrix[y][x];
                var bottom = hasNext && matrix[y + 1][x];
                sb.Append((top, bottom) switch
                {
                    (true, true) => '█',
                    (true, false) => '▀',
                    (false, true) => '▄',
                    _ => ' '
                });
            }

            Console.WriteLine(sb.ToString());
        }
    }

    private static int GetConsoleWidthOrDefault(int fallback = 120)
    {
        try
        {
            return Console.WindowWidth > 0 ? Console.WindowWidth : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static int GetConsoleHeightOrDefault(int fallback = 40)
    {
        try
        {
            return Console.WindowHeight > 0 ? Console.WindowHeight : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static bool TryUpdateCookiesInConfig(
        string platformKey,
        string cookies,
        out string configPath,
        out string error)
    {
        configPath = Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "appsettings.json");
        error = string.Empty;

        try
        {
            if (!File.Exists(configPath))
            {
                error = $"找不到配置文件：{configPath}";
                return false;
            }

            var root = JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject;
            var platforms = root?["Crawler"]?["Platforms"] as JsonObject;
            var platform = platforms?[platformKey] as JsonObject;
            if (platform is null)
            {
                error = $"配置中不存在平台节点：Crawler.Platforms.{platformKey}";
                return false;
            }

            platform["Cookies"] = cookies;
            var json = root!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void ShowHelp()
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
        Console.WriteLine("  xhs      - 小红书");
        Console.WriteLine("  bili     - B站");
        Console.WriteLine("  douyin   - 抖音");
        Console.WriteLine("  tieba    - 贴吧");
        Console.WriteLine("  kuaishou - 快手");
        Console.WriteLine("  zhihu    - 知乎");
        Console.WriteLine("  weibo    - 微博");
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

    private sealed record ParsedArgs(
        List<string> Positionals,
        Dictionary<string, string> Options
    );
}
