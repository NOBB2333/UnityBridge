using SqlSugar;
using UnityBridge.Crawler.Tieba;
using UnityBridge.Crawler.Tieba.Extensions;
using UnityBridge.Crawler.Tieba.Models;

namespace UnityBridge.Crawler;

public static partial class CrawlerCommand
{
    /// <summary>
    /// 贴吧关键词搜索（HTML）。
    /// </summary>
    public static async Task TiebaSearchAsync(
        TiebaClient client,
        string keyword,
        int maxPages = 10,
        CancellationToken ct = default)
    {
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
    public static async Task<string> TiebaGetPostDetailHtmlAsync(
        TiebaClient client,
        string postId,
        CancellationToken ct = default)
    {
        var html = await client.ExecutePostDetailHtmlAsync(new TiebaPostDetailRequest
        {
            PostId = postId
        }, ct);

        Console.WriteLine($"[Tieba] 帖子详情获取成功：{postId}，HTML 长度 {html.Length}");
        return html;
    }

    /// <summary>
    /// 贴吧评论（HTML）。
    /// </summary>
    public static async Task TiebaGetCommentsHtmlAsync(
        TiebaClient client,
        string postId,
        int page = 1,
        string? parentCommentId = null,
        string? tiebaId = null,
        CancellationToken ct = default)
    {
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
    public static async Task TiebaGetCreatorPostsAsync(
        TiebaClient client,
        SqlSugarClient db,
        string userName,
        int maxPages = 2,
        CancellationToken ct = default)
    {
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
    public static async Task<string> TiebaGetForumHtmlAsync(
        TiebaClient client,
        string tiebaName,
        int pageNum = 0,
        CancellationToken ct = default)
    {
        var html = await client.ExecuteForumHtmlAsync(new TiebaForumRequest
        {
            TiebaName = tiebaName,
            PageNum = pageNum
        }, ct);

        Console.WriteLine($"[Tieba] 吧页获取成功：{tiebaName} page={pageNum}，HTML 长度 {html.Length}");
        return html;
    }

    /// <summary>
    /// 贴吧登录状态检测。
    /// </summary>
    public static async Task<bool> TiebaLoginCheckAsync(
        TiebaClient client,
        CancellationToken ct = default)
    {
        var html = await client.ExecuteForumHtmlAsync(new TiebaForumRequest
        {
            TiebaName = "贴吧",
            PageNum = 0
        }, ct);

        return !string.IsNullOrWhiteSpace(html);
    }
}
