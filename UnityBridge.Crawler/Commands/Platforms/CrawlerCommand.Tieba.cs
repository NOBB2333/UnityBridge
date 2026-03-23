using SqlSugar;
using UnityBridge.Crawler.Tieba;
using UnityBridge.Crawler.Tieba.Extensions;
using UnityBridge.Crawler.Tieba.Models;

namespace UnityBridge.Crawler;

[CrawlerPlatform("tieba", "贴吧", "tb", "贴吧")]
public static class TiebaCli
{
    /// <summary>
    /// 贴吧关键词搜索（HTML）。
    /// </summary>
    [CrawlerAction("search", PlatformArgumentIndex = 1, PlatformOptional = true, SupportsAllPlatforms = true, RunInParallelForAll = true)]
    public static async Task SearchAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("贴吧", ctx.Options.Platforms.Tieba)) return;

        var keyword = ctx.RequirePositional(0, "搜索关键词");
        var maxPages = ctx.GetIntOption(ctx.Options.MaxPages, "max-pages");
        var client = CrawlerFactory.CreateTiebaClient(ctx.Options.Platforms.Tieba.Cookies);
        var ct = ctx.CancellationToken;

        Console.WriteLine($"[Tieba] 开始搜索关键词：{keyword}");

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var html = await client.ExecuteSearchHtmlAsync(new TiebaSearchRequest
            {
                Keyword = keyword,
                Page = page,
                PageSize = 20
            }, ct);

            if (string.IsNullOrWhiteSpace(html))
            {
                Console.WriteLine("[Tieba] 没有更多结果。");
                break;
            }

            Console.WriteLine($"[Tieba] 第 {page} 页：HTML 长度 {html.Length}");
            await Task.Delay(Random.Shared.Next(500, 1200), ct);
        }
    }

    /// <summary>
    /// 贴吧帖子详情（HTML）。
    /// </summary>
    [CrawlerAction("detail", PlatformArgumentIndex = 0)]
    public static async Task<string> DetailAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("贴吧", ctx.Options.Platforms.Tieba)) return string.Empty;

        var postId = ctx.RequirePositional(1, "内容ID");
        var client = CrawlerFactory.CreateTiebaClient(ctx.Options.Platforms.Tieba.Cookies);
        var html = await client.ExecutePostDetailHtmlAsync(new TiebaPostDetailRequest
        {
            PostId = postId
        }, ctx.CancellationToken);

        Console.WriteLine($"[Tieba] 帖子详情获取成功：{postId}，HTML 长度 {html.Length}");
        return html;
    }

    /// <summary>
    /// 贴吧评论（HTML）。
    /// </summary>
    [CrawlerAction("comments", PlatformArgumentIndex = 0)]
    public static async Task CommentsAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("贴吧", ctx.Options.Platforms.Tieba)) return;

        var postId = ctx.RequirePositional(1, "内容ID");
        var page = ctx.GetIntOption(1, "page");
        var parentCommentId = ctx.GetOption("parent-comment-id", "parent_comment_id", "pid");
        var tiebaId = ctx.GetOption("tieba-id", "tieba_id", "fid");
        var client = CrawlerFactory.CreateTiebaClient(ctx.Options.Platforms.Tieba.Cookies);
        var ct = ctx.CancellationToken;
        var html = await client.ExecuteCommentHtmlAsync(new TiebaCommentRequest
        {
            PostId = postId,
            Page = page
        }, ct);

        Console.WriteLine($"[Tieba] 评论页获取成功：post={postId} page={page}，HTML 长度 {html.Length}");

        if (!string.IsNullOrWhiteSpace(parentCommentId) && !string.IsNullOrWhiteSpace(tiebaId))
        {
            var subHtml = await client.ExecuteSubCommentHtmlAsync(new TiebaSubCommentRequest
            {
                PostId = postId,
                ParentCommentId = parentCommentId,
                TiebaId = tiebaId,
                Page = 1
            }, ct);
            Console.WriteLine($"[Tieba] 子评论获取成功：parent={parentCommentId}，HTML 长度 {subHtml.Length}");
        }
    }

    /// <summary>
    /// 贴吧创作者帖子并存储。
    /// </summary>
    [CrawlerAction("creator", PlatformArgumentIndex = 0)]
    public static async Task CreatorAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("贴吧", ctx.Options.Platforms.Tieba)) return;

        var userName = ctx.RequirePositional(1, "创作者ID");
        var maxPages = ctx.GetIntOption(2, "max-pages");
        var client = CrawlerFactory.CreateTiebaClient(ctx.Options.Platforms.Tieba.Cookies);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var total = 0;
        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var response = await client.ExecuteCreatorPostsAsync(new TiebaCreatorPostsRequest
            {
                UserName = userName,
                PageNum = page
            }, ct);

            if (!response.IsSuccessful() || response.Data?.ThreadList is not { Count: > 0 } posts)
            {
                break;
            }

            var entities = posts
                .Where(p => !string.IsNullOrWhiteSpace(p.ThreadId))
                .Select(p => new TiebaPost
                {
                    PostId = p.ThreadId!,
                    Title = p.Title,
                    TiebaName = p.ForumName,
                    PublishTime = p.CreateTime,
                    ReplyCount = p.ReplyNum,
                    PostUrl = $"https://tieba.baidu.com/p/{p.ThreadId}",
                    CrawledAt = DateTimeOffset.Now
                })
                .ToList();

            total += await db.Storageable(entities).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Tieba] 创作者第 {page} 页：获取 {entities.Count} 条，累计存储 {total} 条");

            if (response.Data.HasMore == 0)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 贴吧吧页（HTML）。
    /// </summary>
    [CrawlerAction("homefeed", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task<string> HomeFeedAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("贴吧", ctx.Options.Platforms.Tieba)) return string.Empty;

        var tiebaName = ctx.GetOption("tieba-name", "tieba_name") ?? "贴吧";
        var pageNum = ctx.GetIntOption(0, "page");
        var client = CrawlerFactory.CreateTiebaClient(ctx.Options.Platforms.Tieba.Cookies);
        var html = await client.ExecuteForumHtmlAsync(new TiebaForumRequest
        {
            TiebaName = tiebaName,
            PageNum = pageNum
        }, ctx.CancellationToken);

        Console.WriteLine($"[Tieba] 吧页获取成功：{tiebaName} page={pageNum}，HTML 长度 {html.Length}");
        return html;
    }

    /// <summary>
    /// 贴吧登录状态检测。
    /// </summary>
    [CrawlerAction("login-check", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task<bool> LoginCheckAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("贴吧", ctx.Options.Platforms.Tieba)) return false;

        var client = CrawlerFactory.CreateTiebaClient(ctx.Options.Platforms.Tieba.Cookies);
        var html = await client.ExecuteForumHtmlAsync(new TiebaForumRequest
        {
            TiebaName = "贴吧",
            PageNum = 0
        }, ctx.CancellationToken);

        var ok = !string.IsNullOrWhiteSpace(html);
        Console.WriteLine($"[Login] 贴吧: {(ok ? "OK" : "FAIL")}");
        return ok;
    }
}
