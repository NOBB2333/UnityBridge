using SqlSugar;
using UnityBridge.Crawler.Zhihu;
using UnityBridge.Crawler.Zhihu.Extensions;
using UnityBridge.Crawler.Zhihu.Models;

namespace UnityBridge.Crawler;

public static partial class CrawlerCommand
{
    /// <summary>
    /// 知乎关键词搜索并存储。
    /// </summary>
    public static async Task ZhihuSearchAsync(
        ZhihuClient client,
        SqlSugarClient db,
        string keyword,
        int maxPages = 10,
        int delayMinMs = 1000,
        int delayMaxMs = 3000,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[Zhihu] 开始搜索关键词：{keyword}");

        for (int page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            try
            {
                var request = new ZhihuSearchRequest
                {
                    Keyword = keyword,
                    Page = page,
                    PageSize = 20
                };

                var response = await client.ExecuteSearchAsync(request, ct);

                if (!response.IsSuccessful() || response.Data is not { Count: > 0 } results)
                {
                    Console.WriteLine("[Zhihu] 没有更多结果。");
                    break;
                }

                var contents = results
                    .Where(r => r.Object is not null)
                    .Select(r =>
                    {
                        var c = r.Object!;
                        c.Keyword = keyword;
                        c.CrawledAt = DateTimeOffset.Now;

                        if (c.Author is not null)
                        {
                            c.UserId = c.Author.Id;
                            c.UserNickname = c.Author.Name;
                            c.UserUrlToken = c.Author.UrlToken;
                            c.UserAvatar = c.Author.AvatarUrl;
                        }

                        return c;
                    }).ToList();

                var count = await db.Storageable(contents).ExecuteCommandAsync(ct);
                Console.WriteLine($"[Zhihu] 第 {page} 页：获取 {results.Count} 条，存储 {count} 条内容");

                if (response.Paging?.IsEnd == true)
                {
                    Console.WriteLine("[Zhihu] 已到达最后一页。");
                    break;
                }

                await Task.Delay(Random.Shared.Next(delayMinMs, delayMaxMs), ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Zhihu] 搜索已取消。");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Zhihu] 搜索异常：{ex.Message}");
            }
        }

        Console.WriteLine($"[Zhihu] 搜索完成：{keyword}");
    }

    /// <summary>
    /// 知乎内容详情 HTML。
    /// </summary>
    public static async Task<string?> ZhihuGetContentDetailHtmlAsync(
        ZhihuClient client,
        string contentId,
        string detailType,
        string? questionId = null,
        CancellationToken ct = default)
    {
        string html;
        switch (detailType.Trim().ToLowerInvariant())
        {
            case "answer":
                if (string.IsNullOrWhiteSpace(questionId))
                {
                    Console.WriteLine("[Zhihu] answer 详情需要提供 --question-id。");
                    return null;
                }

                html = await client.ExecuteAnswerDetailHtmlAsync(new ZhihuAnswerDetailRequest
                {
                    QuestionId = questionId,
                    AnswerId = contentId
                }, ct);
                break;

            case "video":
                html = await client.ExecuteVideoDetailHtmlAsync(new ZhihuVideoDetailRequest
                {
                    VideoId = contentId
                }, ct);
                break;

            default:
                html = await client.ExecuteArticleDetailHtmlAsync(new ZhihuArticleDetailRequest
                {
                    ArticleId = contentId
                }, ct);
                break;
        }

        Console.WriteLine($"[Zhihu] 详情页面获取成功：type={detailType} id={contentId}，HTML 长度 {html.Length}");
        return html;
    }

    /// <summary>
    /// 知乎评论并存储。
    /// </summary>
    public static async Task ZhihuGetCommentsAsync(
        ZhihuClient client,
        SqlSugarClient db,
        string contentId,
        string contentType = "answer",
        int maxPages = 3,
        bool includeSubComments = true,
        CancellationToken ct = default)
    {
        var offset = string.Empty;
        var total = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var response = await client.ExecuteCommentPageAsync(new ZhihuCommentRequest
            {
                ContentId = contentId,
                ContentType = contentType,
                Offset = offset
            }, ct);

            if (!response.IsSuccessful() || response.Data is not { Count: > 0 } comments)
            {
                break;
            }

            foreach (var comment in comments)
            {
                NormalizeZhihuComment(comment, contentId, contentType, null);
            }

            total += await db.Storageable(comments).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Zhihu] 评论第 {page} 页：获取 {comments.Count} 条，累计存储 {total} 条");

            if (includeSubComments)
            {
                foreach (var root in comments.Where(c => c.SubCommentCount.GetValueOrDefault() > 0))
                {
                    await ZhihuGetSubCommentsAsync(client, db, contentId, contentType, root.CommentId, ct);
                }
            }

            if (response.Paging?.IsEnd == true)
            {
                break;
            }

            offset = GetOffsetFromPagingUrl(response.Paging?.Next);
            if (string.IsNullOrWhiteSpace(offset))
            {
                break;
            }
        }
    }

    /// <summary>
    /// 知乎创作者内容并存储。
    /// </summary>
    public static async Task ZhihuGetCreatorContentsAsync(
        ZhihuClient client,
        SqlSugarClient db,
        string urlToken,
        string creatorType = "answers",
        int maxPages = 2,
        CancellationToken ct = default)
    {
        try
        {
            var profileHtml = await client.ExecuteCreatorProfileHtmlAsync(urlToken, ct);
            Console.WriteLine($"[Zhihu] 创作者页面获取成功：{urlToken}，HTML 长度 {profileHtml.Length}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Zhihu] 创作者页面获取失败：{ex.Message}");
        }

        var offset = 0;
        var total = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            ZhihuCreatorContentResponse response = creatorType switch
            {
                "articles" => await client.ExecuteCreatorArticlesAsync(new ZhihuCreatorContentRequest
                {
                    UrlToken = urlToken,
                    Offset = offset
                }, ct),
                "videos" => await client.ExecuteCreatorVideosAsync(new ZhihuCreatorContentRequest
                {
                    UrlToken = urlToken,
                    Offset = offset
                }, ct),
                _ => await client.ExecuteCreatorAnswersAsync(new ZhihuCreatorContentRequest
                {
                    UrlToken = urlToken,
                    Offset = offset
                }, ct)
            };

            if (!response.IsSuccessful() || response.Data is not { Count: > 0 } contents)
            {
                break;
            }

            foreach (var content in contents)
            {
                content.CrawledAt = DateTimeOffset.Now;
                content.UserUrlToken = urlToken;

                if (content.Author is not null)
                {
                    content.UserId = content.Author.Id;
                    content.UserNickname = content.Author.Name;
                    content.UserAvatar = content.Author.AvatarUrl;
                }
            }

            total += await db.Storageable(contents).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Zhihu] 创作者内容第 {page} 页：获取 {contents.Count} 条，累计存储 {total} 条");

            if (response.Paging?.IsEnd == true)
            {
                break;
            }

            offset += contents.Count;
        }
    }

    /// <summary>
    /// 知乎登录状态检测。
    /// </summary>
    public static async Task<bool> ZhihuLoginCheckAsync(
        ZhihuClient client,
        CancellationToken ct = default)
    {
        var response = await client.ExecuteSearchAsync(new ZhihuSearchRequest
        {
            Keyword = "知乎",
            Page = 1,
            PageSize = 1
        }, ct);

        return response.IsSuccessful();
    }

    private static async Task ZhihuGetSubCommentsAsync(
        ZhihuClient client,
        SqlSugarClient db,
        string contentId,
        string contentType,
        string rootCommentId,
        CancellationToken ct)
    {
        var response = await client.ExecuteSubCommentAsync(new ZhihuSubCommentRequest
        {
            RootCommentId = rootCommentId,
            Offset = string.Empty
        }, ct);

        if (!response.IsSuccessful() || response.Data is not { Count: > 0 } subComments)
        {
            return;
        }

        foreach (var sub in subComments)
        {
            NormalizeZhihuComment(sub, contentId, contentType, rootCommentId);
        }

        await db.Storageable(subComments).ExecuteCommandAsync(ct);
    }

    private static void NormalizeZhihuComment(
        ZhihuComment comment,
        string contentId,
        string contentType,
        string? parentCommentId)
    {
        comment.ContentId = contentId;
        comment.ContentType = contentType;
        comment.ParentCommentId = parentCommentId;
        comment.CrawledAt = DateTimeOffset.Now;

        if (comment.Author is not null)
        {
            comment.UserId = comment.Author.Id;
            comment.UserNickname = comment.Author.Name;
            comment.UserAvatar = comment.Author.AvatarUrl;
        }
    }

    private static string GetOffsetFromPagingUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var query = uri.Query.TrimStart('?');
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = pair.IndexOf('=');
                if (idx <= 0)
                {
                    continue;
                }

                var key = pair[..idx];
                var val = pair[(idx + 1)..];
                if (string.Equals(key, "offset", StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(val);
                }
            }
        }

        return string.Empty;
    }
}
