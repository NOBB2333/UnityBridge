using SqlSugar;
using UnityBridge.Crawler.Douyin;
using UnityBridge.Crawler.Douyin.Extensions;
using UnityBridge.Crawler.Douyin.Models;

namespace UnityBridge.Crawler;

public static partial class CrawlerCommand
{
    /// <summary>
    /// 抖音关键词搜索并存储。
    /// </summary>
    public static async Task DouyinSearchAsync(
        DouyinClient client,
        SqlSugarClient db,
        string keyword,
        int maxPages = 10,
        int delayMinMs = 1000,
        int delayMaxMs = 3000,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[Douyin] 开始搜索关键词：{keyword}");
        int offset = 0;

        for (int page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            try
            {
                var request = new DouyinSearchRequest
                {
                    Keyword = keyword,
                    Offset = offset
                };

                var response = await client.ExecuteSearchAsync(request, ct);

                if (!response.IsSuccessful() || response.Data is not { Count: > 0 } results)
                {
                    Console.WriteLine("[Douyin] 没有更多结果。");
                    break;
                }

                var awemes = results
                    .Where(r => r.AwemeInfo is not null)
                    .Select(r =>
                    {
                        var a = r.AwemeInfo!;
                        a.Keyword = keyword;
                        a.CrawledAt = DateTimeOffset.Now;

                        if (a.Author is not null)
                        {
                            a.UserId = a.Author.Uid;
                            a.Nickname = a.Author.Nickname;
                        }

                        return a;
                    }).ToList();

                var count = await db.Storageable(awemes).ExecuteCommandAsync(ct);
                Console.WriteLine($"[Douyin] 第 {page} 页：获取 {results.Count} 条，存储 {count} 条视频");

                if (response.HasMore == 0)
                {
                    Console.WriteLine("[Douyin] 已到达最后一页。");
                    break;
                }

                offset = response.Cursor;
                await Task.Delay(Random.Shared.Next(delayMinMs, delayMaxMs), ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Douyin] 搜索已取消。");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Douyin] 搜索异常：{ex.Message}");
            }
        }

        Console.WriteLine($"[Douyin] 搜索完成：{keyword}");
    }

    /// <summary>
    /// 抖音视频详情并存储。
    /// </summary>
    public static async Task<DouyinAweme?> DouyinGetAwemeDetailAsync(
        DouyinClient client,
        SqlSugarClient db,
        string awemeId,
        CancellationToken ct = default)
    {
        var response = await client.ExecuteAwemeDetailAsync(new DouyinAwemeDetailRequest
        {
            AwemeId = awemeId
        }, ct);

        if (!response.IsSuccessful() || response.AwemeDetail is null)
        {
            Console.WriteLine($"[Douyin] 获取详情失败：{awemeId}");
            return null;
        }

        var aweme = response.AwemeDetail;
        NormalizeDouyinAweme(aweme);
        await db.Storageable(aweme).ExecuteCommandAsync(ct);

        Console.WriteLine($"[Douyin] 详情成功：{aweme.AwemeId} {aweme.Title}");
        return aweme;
    }

    /// <summary>
    /// 抖音评论并存储。
    /// </summary>
    public static async Task DouyinGetCommentsAsync(
        DouyinClient client,
        SqlSugarClient db,
        string awemeId,
        int maxPages = 3,
        bool includeSubComments = true,
        CancellationToken ct = default)
    {
        var cursor = 0;
        var total = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var response = await client.ExecuteCommentPageAsync(new DouyinCommentRequest
            {
                AwemeId = awemeId,
                Cursor = cursor
            }, ct);

            if (!response.IsSuccessful() || response.Comments is not { Count: > 0 } comments)
            {
                break;
            }

            foreach (var comment in comments)
            {
                NormalizeDouyinComment(comment, awemeId, null);
            }

            total += await db.Storageable(comments).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Douyin] 评论第 {page} 页：获取 {comments.Count} 条，累计存储 {total} 条");

            if (includeSubComments)
            {
                foreach (var root in comments.Where(c => c.ReplyCount.GetValueOrDefault() > 0))
                {
                    await DouyinGetSubCommentsAsync(client, db, awemeId, root.Cid, ct);
                }
            }

            if (response.HasMore == 0)
            {
                break;
            }

            cursor = (int)response.Cursor;
            await Task.Delay(Random.Shared.Next(400, 1000), ct);
        }
    }

    /// <summary>
    /// 抖音创作者信息与作品并存储。
    /// </summary>
    public static async Task DouyinGetCreatorAsync(
        DouyinClient client,
        SqlSugarClient db,
        string secUserId,
        int maxPages = 2,
        CancellationToken ct = default)
    {
        var profile = await client.ExecuteUserProfileAsync(new DouyinUserProfileRequest
        {
            SecUserId = secUserId
        }, ct);

        if (profile.IsSuccessful() && profile.User is not null)
        {
            var creator = profile.User;
            creator.CrawledAt = DateTimeOffset.Now;
            creator.Avatar = creator.AvatarThumb?.UrlList?.FirstOrDefault();
            await db.Storageable(creator).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Douyin] 创作者信息已保存：{creator.Nickname} ({creator.SecUid})");
        }
        else
        {
            Console.WriteLine("[Douyin] 获取创作者信息失败。");
        }

        var maxCursor = "0";
        var saved = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var posts = await client.ExecuteUserPostsAsync(new DouyinUserPostsRequest
            {
                SecUserId = secUserId,
                MaxCursor = maxCursor
            }, ct);

            if (!posts.IsSuccessful() || posts.AwemeList is not { Count: > 0 } awemes)
            {
                break;
            }

            foreach (var aweme in awemes)
            {
                NormalizeDouyinAweme(aweme);
            }

            saved += await db.Storageable(awemes).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Douyin] 创作者作品第 {page} 页：获取 {awemes.Count} 条，累计存储 {saved} 条");

            if (posts.HasMore == 0)
            {
                break;
            }

            maxCursor = posts.MaxCursor.ToString();
        }
    }

    /// <summary>
    /// 抖音首页推荐并存储。
    /// </summary>
    public static async Task DouyinGetHomeFeedAsync(
        DouyinClient client,
        SqlSugarClient db,
        int count = 20,
        CancellationToken ct = default)
    {
        var response = await client.ExecuteHomeFeedAsync(new DouyinHomeFeedRequest
        {
            Count = count
        }, ct);

        if (!response.IsSuccessful() || response.AwemeList is not { Count: > 0 } awemes)
        {
            Console.WriteLine("[Douyin] 获取首页推荐失败。");
            return;
        }

        foreach (var aweme in awemes)
        {
            NormalizeDouyinAweme(aweme);
        }

        var saved = await db.Storageable(awemes).ExecuteCommandAsync(ct);
        Console.WriteLine($"[Douyin] 首页推荐：获取 {awemes.Count} 条，存储 {saved} 条");
    }

    /// <summary>
    /// 抖音登录状态检测。
    /// </summary>
    public static async Task<bool> DouyinLoginCheckAsync(
        DouyinClient client,
        CancellationToken ct = default)
    {
        var response = await client.ExecuteHomeFeedAsync(new DouyinHomeFeedRequest
        {
            Count = 1
        }, ct);

        return response.IsSuccessful();
    }

    private static async Task DouyinGetSubCommentsAsync(
        DouyinClient client,
        SqlSugarClient db,
        string awemeId,
        string commentId,
        CancellationToken ct)
    {
        var response = await client.ExecuteSubCommentAsync(new DouyinSubCommentRequest
        {
            AwemeId = awemeId,
            CommentId = commentId,
            Cursor = 0
        }, ct);

        if (!response.IsSuccessful() || response.Comments is not { Count: > 0 } subComments)
        {
            return;
        }

        foreach (var sub in subComments)
        {
            NormalizeDouyinComment(sub, awemeId, commentId);
        }

        await db.Storageable(subComments).ExecuteCommandAsync(ct);
    }

    private static void NormalizeDouyinAweme(DouyinAweme aweme)
    {
        aweme.CrawledAt = DateTimeOffset.Now;

        if (aweme.Author is not null)
        {
            aweme.UserId = aweme.Author.Uid;
            aweme.SecUid = aweme.Author.SecUid;
            aweme.Nickname = aweme.Author.Nickname;
            aweme.Avatar = aweme.Author.AvatarThumb?.UrlList?.FirstOrDefault();
        }

        if (aweme.Statistics is not null)
        {
            aweme.DiggCount = aweme.Statistics.DiggCount;
            aweme.CommentCount = aweme.Statistics.CommentCount;
            aweme.ShareCount = aweme.Statistics.ShareCount;
            aweme.CollectCount = aweme.Statistics.CollectCount;
            aweme.PlayCount = aweme.Statistics.PlayCount;
        }

        if (aweme.Video is not null)
        {
            aweme.CoverUrl = aweme.Video.Cover?.UrlList?.FirstOrDefault();
            aweme.VideoUrl = aweme.Video.PlayAddr?.UrlList?.FirstOrDefault();
        }
    }

    private static void NormalizeDouyinComment(DouyinComment comment, string awemeId, string? parentCommentId)
    {
        comment.AwemeId = awemeId;
        comment.ReplyId = parentCommentId;
        comment.CrawledAt = DateTimeOffset.Now;

        if (comment.User is not null)
        {
            comment.UserId = comment.User.Uid;
            comment.SecUid = comment.User.SecUid;
            comment.Nickname = comment.User.Nickname;
            comment.Avatar = comment.User.AvatarThumb?.UrlList?.FirstOrDefault();
        }
    }
}
