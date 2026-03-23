using SqlSugar;
using UnityBridge.Crawler.Weibo;
using UnityBridge.Crawler.Weibo.Extensions;
using UnityBridge.Crawler.Weibo.Models;

namespace UnityBridge.Crawler;

[CrawlerPlatform("weibo", "微博", "wb", "微博")]
public static class WeiboCli
{
    /// <summary>
    /// 微博关键词搜索并存储。
    /// </summary>
    [CrawlerAction("search", PlatformArgumentIndex = 1, PlatformOptional = true, SupportsAllPlatforms = true, RunInParallelForAll = true)]
    public static async Task SearchAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("微博", ctx.Options.Platforms.Weibo)) return;

        var keyword = ctx.RequirePositional(0, "搜索关键词");
        var maxPages = ctx.GetIntOption(ctx.Options.MaxPages, "max-pages");
        var delayMinMs = ctx.Options.DefaultDelay.MinMs;
        var delayMaxMs = ctx.Options.DefaultDelay.MaxMs;
        var ct = ctx.CancellationToken;
        var client = CrawlerFactory.CreateWeiboClient(ctx.Options.Platforms.Weibo.Cookies);
        var db = ctx.Db;

        Console.WriteLine($"[Weibo] 开始搜索关键词：{keyword}");

        for (int page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            try
            {
                var request = new WeiboSearchRequest
                {
                    Keyword = keyword,
                    Page = page
                };

                var response = await client.ExecuteSearchAsync(request, ct);

                if (!response.IsSuccessful() || response.Data?.Cards is not { Count: > 0 } cards)
                {
                    Console.WriteLine("[Weibo] 没有更多结果。");
                    break;
                }

                var notes = new List<WeiboNote>();
                foreach (var card in cards)
                {
                    if (card.CardType == 9 && card.Mblog is not null)
                    {
                        var note = card.Mblog;
                        note.Keyword = keyword;
                        note.CrawledAt = DateTimeOffset.Now;

                        if (note.User is not null)
                        {
                            note.UserId = note.User.Id.ToString();
                            note.Nickname = note.User.ScreenName;
                            note.Avatar = note.User.ProfileImageUrl;
                        }

                        notes.Add(note);
                    }

                    if (card.CardGroup is { Count: > 0 })
                    {
                        foreach (var subCard in card.CardGroup)
                        {
                            if (subCard.CardType == 9 && subCard.Mblog is not null)
                            {
                                var note = subCard.Mblog;
                                note.Keyword = keyword;
                                note.CrawledAt = DateTimeOffset.Now;

                                if (note.User is not null)
                                {
                                    note.UserId = note.User.Id.ToString();
                                    note.Nickname = note.User.ScreenName;
                                    note.Avatar = note.User.ProfileImageUrl;
                                }

                                notes.Add(note);
                            }
                        }
                    }
                }

                if (notes.Count == 0)
                {
                    Console.WriteLine("[Weibo] 没有更多结果。");
                    break;
                }

                var count = await db.Storageable(notes).ExecuteCommandAsync(ct);
                Console.WriteLine($"[Weibo] 第 {page} 页：获取 {notes.Count} 条，存储 {count} 条微博");

                await Task.Delay(Random.Shared.Next(delayMinMs, delayMaxMs), ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Weibo] 搜索已取消。");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weibo] 搜索异常：{ex.Message}");
            }
        }

        Console.WriteLine($"[Weibo] 搜索完成：{keyword}");
    }

    /// <summary>
    /// 微博详情 HTML。
    /// </summary>
    [CrawlerAction("detail", PlatformArgumentIndex = 0)]
    public static async Task<string> DetailAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("微博", ctx.Options.Platforms.Weibo)) return string.Empty;

        var noteId = ctx.RequirePositional(1, "内容ID");
        var client = CrawlerFactory.CreateWeiboClient(ctx.Options.Platforms.Weibo.Cookies);
        var html = await client.ExecuteNoteDetailHtmlAsync(new WeiboNoteDetailRequest
        {
            NoteId = noteId
        }, ctx.CancellationToken);

        Console.WriteLine($"[Weibo] 详情页面获取成功：{noteId}，HTML 长度 {html.Length}");
        return html;
    }

    /// <summary>
    /// 微博评论并存储。
    /// </summary>
    [CrawlerAction("comments", PlatformArgumentIndex = 0)]
    public static async Task CommentsAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("微博", ctx.Options.Platforms.Weibo)) return;

        var noteId = ctx.RequirePositional(1, "内容ID");
        var maxPages = ctx.GetIntOption(3, "max-pages");
        var client = CrawlerFactory.CreateWeiboClient(ctx.Options.Platforms.Weibo.Cookies);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        long maxId = 0;
        var maxIdType = 0;
        var total = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var response = await client.ExecuteCommentPageAsync(new WeiboCommentRequest
            {
                NoteId = noteId,
                MaxId = maxId,
                MaxIdType = maxIdType
            }, ct);

            if (!response.IsSuccessful() || response.Data?.Comments is not { Count: > 0 } comments)
            {
                break;
            }

            foreach (var comment in comments)
            {
                comment.NoteId = noteId;
                comment.CrawledAt = DateTimeOffset.Now;
                if (comment.User is not null)
                {
                    comment.UserId = comment.User.Id.ToString();
                    comment.Nickname = comment.User.ScreenName;
                    comment.Avatar = comment.User.ProfileImageUrl;
                }
            }

            total += await db.Storageable(comments).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Weibo] 评论第 {page} 页：获取 {comments.Count} 条，累计存储 {total} 条");

            maxId = response.Data.MaxId;
            maxIdType = response.Data.MaxIdType;
            if (maxId <= 0)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 微博创作者信息与内容并存储。
    /// </summary>
    [CrawlerAction("creator", PlatformArgumentIndex = 0)]
    public static async Task CreatorAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("微博", ctx.Options.Platforms.Weibo)) return;

        var creatorId = ctx.RequirePositional(1, "创作者ID");
        var maxPages = ctx.GetIntOption(2, "max-pages");
        var containerId = ctx.GetOption("container-id", "container_id");
        var client = CrawlerFactory.CreateWeiboClient(ctx.Options.Platforms.Weibo.Cookies);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var profile = await client.ExecuteCreatorProfileAsync(new WeiboCreatorProfileRequest
        {
            CreatorId = creatorId
        }, ct);

        if (profile.IsSuccessful() && profile.Data?.UserInfo is not null)
        {
            var creator = profile.Data.UserInfo;
            creator.CrawledAt = DateTimeOffset.Now;
            await db.Storageable(creator).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Weibo] 创作者信息已保存：{creator.Nickname} ({creator.UserId})");
        }
        else
        {
            Console.WriteLine("[Weibo] 获取创作者信息失败。");
        }

        if (string.IsNullOrWhiteSpace(containerId))
        {
            containerId = profile.Data?.TabsInfo?.Tabs?
                .FirstOrDefault(t => string.Equals(t.TabKey, "weibo", StringComparison.OrdinalIgnoreCase))
                ?.ContainerId
                ?? profile.Data?.TabsInfo?.Tabs?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.ContainerId))?.ContainerId;
        }

        if (string.IsNullOrWhiteSpace(containerId))
        {
            Console.WriteLine("[Weibo] 未获取到 containerId，跳过创作者微博列表。");
            return;
        }

        var sinceId = "0";
        var total = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var notesResponse = await client.ExecuteCreatorNotesAsync(new WeiboCreatorNotesRequest
            {
                CreatorId = creatorId,
                ContainerId = containerId,
                SinceId = sinceId
            }, ct);

            if (!notesResponse.IsSuccessful() || notesResponse.Data?.Cards is not { Count: > 0 } cards)
            {
                break;
            }

            var notes = FlattenWeiboCards(cards);
            if (notes.Count == 0)
            {
                break;
            }

            total += await db.Storageable(notes).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Weibo] 创作者内容第 {page} 页：获取 {notes.Count} 条，累计存储 {total} 条");

            if (string.IsNullOrWhiteSpace(notesResponse.Data.SinceId) || notesResponse.Data.SinceId == sinceId)
            {
                break;
            }

            sinceId = notesResponse.Data.SinceId;
        }
    }

    /// <summary>
    /// 微博登录状态检测。
    /// </summary>
    [CrawlerAction("login-check", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task<bool> LoginCheckAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("微博", ctx.Options.Platforms.Weibo)) return false;

        var client = CrawlerFactory.CreateWeiboClient(ctx.Options.Platforms.Weibo.Cookies);
        var response = await client.ExecuteSearchAsync(new WeiboSearchRequest
        {
            Keyword = "微博",
            Page = 1
        }, ctx.CancellationToken);

        var ok = response.IsSuccessful();
        Console.WriteLine($"[Login] 微博: {(ok ? "OK" : "FAIL")}");
        return ok;
    }

    private static List<WeiboNote> FlattenWeiboCards(List<WeiboCard> cards)
    {
        var notes = new List<WeiboNote>();

        foreach (var card in cards)
        {
            if (card.CardType == 9 && card.Mblog is not null)
            {
                var note = card.Mblog;
                NormalizeWeiboNote(note);
                notes.Add(note);
            }

            if (card.CardGroup is not { Count: > 0 })
            {
                continue;
            }

            foreach (var sub in card.CardGroup)
            {
                if (sub.CardType == 9 && sub.Mblog is not null)
                {
                    var note = sub.Mblog;
                    NormalizeWeiboNote(note);
                    notes.Add(note);
                }
            }
        }

        return notes;
    }

    private static void NormalizeWeiboNote(WeiboNote note)
    {
        note.CrawledAt = DateTimeOffset.Now;
        note.NoteUrl = $"https://m.weibo.cn/detail/{note.NoteId}";
        if (note.Pics is { Count: > 0 })
        {
            note.ImageList = string.Join(",", note.Pics.Select(p => p.Url));
        }

        if (note.User is not null)
        {
            note.UserId = note.User.Id.ToString();
            note.Nickname = note.User.ScreenName;
            note.Avatar = note.User.ProfileImageUrl;
        }
    }

}
