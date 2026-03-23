using SqlSugar;
using UnityBridge.Crawler.XiaoHongShu;
using UnityBridge.Crawler.XiaoHongShu.Extensions;
using UnityBridge.Crawler.XiaoHongShu.Models;

namespace UnityBridge.Crawler;

[CrawlerPlatform("xhs", "小红书", "xiaohongshu", "小红书")]
public static class XhsCli
{
    /// <summary>
    /// 小红书关键词搜索并存储。
    /// </summary>
    [CrawlerAction("search", PlatformArgumentIndex = 1, PlatformOptional = true, SupportsAllPlatforms = true, RunInParallelForAll = true)]
    public static async Task SearchAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("小红书", ctx.Options.Platforms.XiaoHongShu)) return;

        var keyword = ctx.RequirePositional(0, "搜索关键词");
        var maxPages = ctx.GetIntOption(ctx.Options.MaxPages, "max-pages");
        var delayMinMs = ctx.Options.DefaultDelay.MinMs;
        var delayMaxMs = ctx.Options.DefaultDelay.MaxMs;
        var ct = ctx.CancellationToken;
        var client = CrawlerFactory.CreateXhsClient(ctx.Options.Platforms.XiaoHongShu.Cookies, ctx.Options.SignServerUrl);
        var db = ctx.Db;

        Console.WriteLine($"[XHS] 开始搜索关键词：{keyword}");

        for (int page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            try
            {
                var request = new XhsNoteSearchRequest
                {
                    Keyword = keyword,
                    Page = page,
                    PageSize = 20
                };

                var response = await client.ExecuteNoteSearchAsync(request, ct);

                if (!response.IsSuccessful())
                {
                    Console.WriteLine($"[XHS] 搜索失败：{response.Code} {response.Message}");

                    if (await client.SwitchToNextAccountAsync(ct))
                    {
                        Console.WriteLine("[XHS] 已切换到新账号，重试...");
                        page--;
                        continue;
                    }
                    break;
                }

                if (response.Data?.Items is not { Count: > 0 } items)
                {
                    Console.WriteLine("[XHS] 没有更多结果。");
                    break;
                }

                var notes = items
                    .Where(i => i.NoteCard is not null)
                    .Select(i =>
                    {
                        var note = i.NoteCard!;
                        note.XsecToken = i.XsecToken;
                        note.Keyword = keyword;
                        note.CrawledAt = DateTimeOffset.Now;

                        if (note.User is not null)
                        {
                            note.UserId = note.User.UserId;
                            note.UserNickname = note.User.Nickname;
                        }

                        if (note.ImageList is { Count: > 0 })
                        {
                            note.ImageUrls = string.Join(",", note.ImageList.Select(img => img.Url));
                        }

                        if (note.Video is not null)
                        {
                            note.VideoUrl = note.Video.Url;
                        }

                        if (note.InteractInfo is not null)
                        {
                            long.TryParse(note.InteractInfo.LikedCount, out var liked);
                            long.TryParse(note.InteractInfo.CollectedCount, out var collected);
                            long.TryParse(note.InteractInfo.CommentCount, out var comment);
                            long.TryParse(note.InteractInfo.ShareCount, out var share);
                            note.LikedCount = liked;
                            note.CollectedCount = collected;
                            note.CommentCount = comment;
                            note.ShareCount = share;
                        }

                        return note;
                    })
                    .ToList();

                var count = await db.Storageable(notes).ExecuteCommandAsync(ct);
                Console.WriteLine($"[XHS] 第 {page} 页：获取 {items.Count} 条，存储 {count} 条笔记");

                if (!response.Data.HasMore)
                {
                    Console.WriteLine("[XHS] 已到达最后一页。");
                    break;
                }

                await Task.Delay(Random.Shared.Next(delayMinMs, delayMaxMs), ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[XHS] 搜索已取消。");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[XHS] 搜索异常：{ex.Message}");
                await client.MarkCurrentAccountInvalidAsync(ct);
            }
        }

        Console.WriteLine($"[XHS] 搜索完成：{keyword}");
    }

    /// <summary>
    /// 小红书获取笔记详情。
    /// </summary>
    [CrawlerAction("detail", PlatformArgumentIndex = 0)]
    public static async Task<XhsNoteCard?> DetailAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("小红书", ctx.Options.Platforms.XiaoHongShu)) return null;

        var noteId = ctx.RequirePositional(1, "内容ID");
        var xsecToken = ctx.GetOption("xsec-token", "xsec_token");
        var client = CrawlerFactory.CreateXhsClient(ctx.Options.Platforms.XiaoHongShu.Cookies, ctx.Options.SignServerUrl);
        var request = new XhsNoteDetailRequest
        {
            NoteId = noteId,
            XsecToken = xsecToken
        };

        var response = await client.ExecuteNoteDetailAsync(request, ctx.CancellationToken);

        if (!response.IsSuccessful() || response.Data?.Items is not { Count: > 0 })
        {
            Console.WriteLine($"[XHS] 获取笔记详情失败：{noteId}");
            return null;
        }

        var note = response.Data.Items[0].NoteCard;
        if (note is not null)
        {
            note.XsecToken = xsecToken;
            note.CrawledAt = DateTimeOffset.Now;
            note.NoteUrl = $"https://www.xiaohongshu.com/explore/{noteId}";

            await ctx.Db.Storageable(note).ExecuteCommandAsync(ctx.CancellationToken);
        }

        return note;
    }

    /// <summary>
    /// 小红书获取笔记评论（含二级）。
    /// </summary>
    [CrawlerAction("comments", PlatformArgumentIndex = 0)]
    public static async Task CommentsAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("小红书", ctx.Options.Platforms.XiaoHongShu)) return;

        var noteId = ctx.RequirePositional(1, "内容ID");
        var xsecToken = ctx.GetOption("xsec-token", "xsec_token");
        var includeSubComments = ctx.GetBoolOption(true, "include-sub");
        var client = CrawlerFactory.CreateXhsClient(ctx.Options.Platforms.XiaoHongShu.Cookies, ctx.Options.SignServerUrl);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        Console.WriteLine($"[XHS] 开始获取评论：{noteId}");

        string cursor = string.Empty;
        int totalComments = 0;

        while (!ct.IsCancellationRequested)
        {
            var request = new XhsCommentPageRequest
            {
                NoteId = noteId,
                Cursor = cursor,
                XsecToken = xsecToken
            };

            var response = await client.ExecuteCommentPageAsync(request, ct);

            if (!response.IsSuccessful() || response.Data?.Comments is not { Count: > 0 } comments)
            {
                break;
            }

            foreach (var comment in comments)
            {
                comment.NoteId = noteId;
                comment.XsecToken = xsecToken;
                comment.CrawledAt = DateTimeOffset.Now;

                if (comment.UserInfo is not null)
                {
                    comment.UserId = comment.UserInfo.UserId;
                    comment.UserNickname = comment.UserInfo.Nickname;
                }
            }

            var count = await db.Storageable(comments).ExecuteCommandAsync(ct);
            totalComments += count;

            if (includeSubComments)
            {
                foreach (var comment in comments)
                {
                    if (int.TryParse(comment.SubCommentCount, out var subCount) && subCount > 0)
                    {
                        await XhsGetSubCommentsAsync(client, db, noteId, comment.CommentId, xsecToken, ct);
                    }
                }
            }

            if (!response.Data.HasMore || string.IsNullOrEmpty(response.Data.Cursor))
            {
                break;
            }

            cursor = response.Data.Cursor;
            await Task.Delay(Random.Shared.Next(500, 1500), ct);
        }

        Console.WriteLine($"[XHS] 评论获取完成：共 {totalComments} 条");
    }

    private static async Task XhsGetSubCommentsAsync(
        XhsClient client,
        SqlSugarClient db,
        string noteId,
        string rootCommentId,
        string? xsecToken,
        CancellationToken ct)
    {
        string cursor = string.Empty;

        while (!ct.IsCancellationRequested)
        {
            var request = new XhsSubCommentRequest
            {
                NoteId = noteId,
                RootCommentId = rootCommentId,
                Cursor = cursor,
                XsecToken = xsecToken
            };

            var response = await client.ExecuteSubCommentAsync(request, ct);

            if (!response.IsSuccessful() || response.Data?.Comments is not { Count: > 0 } subComments)
            {
                break;
            }

            foreach (var sub in subComments)
            {
                sub.NoteId = noteId;
                sub.ParentCommentId = rootCommentId;
                sub.XsecToken = xsecToken;
                sub.CrawledAt = DateTimeOffset.Now;

                if (sub.UserInfo is not null)
                {
                    sub.UserId = sub.UserInfo.UserId;
                    sub.UserNickname = sub.UserInfo.Nickname;
                }
            }

            await db.Storageable(subComments).ExecuteCommandAsync(ct);

            if (!response.Data.HasMore || string.IsNullOrEmpty(response.Data.Cursor))
            {
                break;
            }

            cursor = response.Data.Cursor;
            await Task.Delay(Random.Shared.Next(300, 800), ct);
        }
    }

    /// <summary>
    /// 小红书首页流并存储。
    /// </summary>
    [CrawlerAction("homefeed", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task HomeFeedAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("小红书", ctx.Options.Platforms.XiaoHongShu)) return;

        var count = ctx.GetIntOption(12, "count");
        var client = CrawlerFactory.CreateXhsClient(ctx.Options.Platforms.XiaoHongShu.Cookies, ctx.Options.SignServerUrl);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var response = await client.ExecuteHomeFeedAsync(new XhsHomeFeedRequest
        {
            Num = count
        }, ct);

        if (!response.IsSuccessful() || response.Data?.Items is not { Count: > 0 } items)
        {
            Console.WriteLine("[XHS] 获取首页流失败。");
            return;
        }

        var notes = items
            .Where(i => i.NoteCard is not null)
            .Select(i => NormalizeXhsNote(i.NoteCard!, i.XsecToken))
            .ToList();

        var saved = await db.Storageable(notes).ExecuteCommandAsync(ct);
        Console.WriteLine($"[XHS] 首页流获取成功：{items.Count} 条，存储 {saved} 条");
    }

    /// <summary>
    /// 小红书创作者笔记并存储。
    /// </summary>
    [CrawlerAction("creator", PlatformArgumentIndex = 0)]
    public static async Task CreatorAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("小红书", ctx.Options.Platforms.XiaoHongShu)) return;

        var userId = ctx.RequirePositional(1, "创作者ID");
        var maxPages = ctx.GetIntOption(2, "max-pages");
        var xsecToken = ctx.GetOption("xsec-token", "xsec_token");
        var client = CrawlerFactory.CreateXhsClient(ctx.Options.Platforms.XiaoHongShu.Cookies, ctx.Options.SignServerUrl);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        Console.WriteLine($"[XHS] 开始获取创作者内容：{userId}");

        var cursor = string.Empty;
        var totalSaved = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var request = new XhsCreatorNotesRequest
            {
                UserId = userId,
                Cursor = cursor,
                Num = 30,
                XsecToken = xsecToken
            };

            var response = await client.ExecuteCreatorNotesAsync(request, ct);
            if (!response.IsSuccessful() || response.Data?.Notes is not { Count: > 0 } notes)
            {
                break;
            }

            foreach (var note in notes)
            {
                NormalizeXhsNote(note, xsecToken);
            }

            totalSaved += await db.Storageable(notes).ExecuteCommandAsync(ct);
            Console.WriteLine($"[XHS] 创作者第 {page} 页：获取 {notes.Count} 条，累计存储 {totalSaved} 条");

            if (!response.Data.HasMore || string.IsNullOrEmpty(response.Data.Cursor))
            {
                break;
            }

            cursor = response.Data.Cursor;
            await Task.Delay(Random.Shared.Next(500, 1200), ct);
        }
    }

    /// <summary>
    /// 小红书登录状态检测。
    /// </summary>
    [CrawlerAction("login-check", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task<bool> LoginCheckAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("小红书", ctx.Options.Platforms.XiaoHongShu)) return false;

        var client = CrawlerFactory.CreateXhsClient(ctx.Options.Platforms.XiaoHongShu.Cookies, ctx.Options.SignServerUrl);
        var response = await client.ExecuteHomeFeedAsync(new XhsHomeFeedRequest
        {
            Num = 1
        }, ctx.CancellationToken);

        var ok = response.IsSuccessful();
        Console.WriteLine($"[Login] 小红书: {(ok ? "OK" : "FAIL")}");
        return ok;
    }

    private static XhsNoteCard NormalizeXhsNote(XhsNoteCard note, string? xsecToken)
    {
        note.XsecToken = string.IsNullOrEmpty(xsecToken) ? note.XsecToken : xsecToken;
        note.CrawledAt = DateTimeOffset.Now;

        if (note.User is not null)
        {
            note.UserId = note.User.UserId;
            note.UserNickname = note.User.Nickname;
        }

        if (note.ImageList is { Count: > 0 })
        {
            note.ImageUrls = string.Join(",", note.ImageList.Select(img => img.Url));
        }

        if (note.Video is not null)
        {
            note.VideoUrl = note.Video.Url;
        }

        if (note.InteractInfo is not null)
        {
            long.TryParse(note.InteractInfo.LikedCount, out var liked);
            long.TryParse(note.InteractInfo.CollectedCount, out var collected);
            long.TryParse(note.InteractInfo.CommentCount, out var comment);
            long.TryParse(note.InteractInfo.ShareCount, out var share);
            note.LikedCount = liked;
            note.CollectedCount = collected;
            note.CommentCount = comment;
            note.ShareCount = share;
        }

        return note;
    }

}
