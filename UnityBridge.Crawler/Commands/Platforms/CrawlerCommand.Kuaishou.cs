using SqlSugar;
using UnityBridge.Crawler.Kuaishou;
using UnityBridge.Crawler.Kuaishou.Extensions;
using UnityBridge.Crawler.Kuaishou.Models;

namespace UnityBridge.Crawler;

[CrawlerPlatform("kuaishou", "快手", "ks", "快手")]
public static class KuaishouCli
{
    /// <summary>
    /// 快手关键词搜索并存储。
    /// </summary>
    [CrawlerAction("search", PlatformArgumentIndex = 1, PlatformOptional = true, SupportsAllPlatforms = true, RunInParallelForAll = true)]
    public static async Task SearchAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("快手", ctx.Options.Platforms.Kuaishou)) return;

        var keyword = ctx.RequirePositional(0, "搜索关键词");
        var maxPages = ctx.GetIntOption(ctx.Options.MaxPages, "max-pages");
        var delayMinMs = ctx.Options.DefaultDelay.MinMs;
        var delayMaxMs = ctx.Options.DefaultDelay.MaxMs;
        var client = CrawlerFactory.CreateKuaishouClient(ctx.Options.Platforms.Kuaishou.Cookies);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        Console.WriteLine($"[Kuaishou] 开始搜索关键词：{keyword}");
        string pcursor = string.Empty;
        string searchSessionId = string.Empty;

        for (int page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            try
            {
                var request = new KuaishouSearchRequest
                {
                    Keyword = keyword,
                    Pcursor = pcursor,
                    SearchSessionId = searchSessionId
                };

                var response = await client.ExecuteSearchAsync(request, ct);

                if (!response.IsSuccessful() || response.Data?.VisionSearchPhoto?.Feeds is not { Count: > 0 } feeds)
                {
                    Console.WriteLine("[Kuaishou] 没有更多结果。");
                    break;
                }

                var videos = feeds
                    .Where(f => f.Photo is not null)
                    .Select(f =>
                    {
                        var v = f.Photo!;
                        v.Keyword = keyword;
                        v.CrawledAt = DateTimeOffset.Now;

                        if (v.Author is not null)
                        {
                            v.UserId = v.Author.Id;
                            v.Nickname = v.Author.Name;
                            v.Avatar = v.Author.HeaderUrl;
                        }

                        return v;
                    }).ToList();

                var count = await db.Storageable(videos).ExecuteCommandAsync(ct);
                Console.WriteLine($"[Kuaishou] 第 {page} 页：获取 {feeds.Count} 条，存储 {count} 条视频");

                pcursor = response.Data.VisionSearchPhoto.Pcursor ?? string.Empty;
                searchSessionId = response.Data.VisionSearchPhoto.SearchSessionId ?? string.Empty;

                if (string.IsNullOrEmpty(pcursor))
                {
                    Console.WriteLine("[Kuaishou] 已到达最后一页。");
                    break;
                }

                await Task.Delay(Random.Shared.Next(delayMinMs, delayMaxMs), ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Kuaishou] 搜索已取消。");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Kuaishou] 搜索异常：{ex.Message}");
            }
        }

        Console.WriteLine($"[Kuaishou] 搜索完成：{keyword}");
    }

    /// <summary>
    /// 快手视频详情并存储。
    /// </summary>
    [CrawlerAction("detail", PlatformArgumentIndex = 0)]
    public static async Task<KuaishouVideo?> DetailAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("快手", ctx.Options.Platforms.Kuaishou)) return null;

        var photoId = ctx.RequirePositional(1, "内容ID");
        var client = CrawlerFactory.CreateKuaishouClient(ctx.Options.Platforms.Kuaishou.Cookies);
        var response = await client.ExecuteVideoDetailAsync(new KuaishouVideoDetailRequest
        {
            PhotoId = photoId
        }, ctx.CancellationToken);

        if (!response.IsSuccessful() || response.Data?.VisionVideoDetail is null)
        {
            Console.WriteLine($"[Kuaishou] 获取详情失败：{photoId}");
            return null;
        }

        var video = response.Data.VisionVideoDetail;
        NormalizeKuaishouVideo(video);
        await ctx.Db.Storageable(video).ExecuteCommandAsync(ctx.CancellationToken);
        Console.WriteLine($"[Kuaishou] 详情成功：{video.PhotoId} {video.Title}");
        return video;
    }

    /// <summary>
    /// 快手评论并存储。
    /// </summary>
    [CrawlerAction("comments", PlatformArgumentIndex = 0)]
    public static async Task CommentsAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("快手", ctx.Options.Platforms.Kuaishou)) return;

        var photoId = ctx.RequirePositional(1, "内容ID");
        var maxPages = ctx.GetIntOption(3, "max-pages");
        var includeSubComments = ctx.GetBoolOption(true, "include-sub");
        var client = CrawlerFactory.CreateKuaishouClient(ctx.Options.Platforms.Kuaishou.Cookies);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var pcursor = string.Empty;
        var total = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var response = await client.ExecuteCommentPageAsync(new KuaishouCommentRequest
            {
                PhotoId = photoId,
                Pcursor = pcursor
            }, ct);

            if (!response.IsSuccessful() || response.Data?.VisionCommentList?.RootComments is not { Count: > 0 } comments)
            {
                break;
            }

            foreach (var comment in comments)
            {
                NormalizeKuaishouComment(comment, photoId, null);
            }

            total += await db.Storageable(comments).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Kuaishou] 评论第 {page} 页：获取 {comments.Count} 条，累计存储 {total} 条");

            if (includeSubComments)
            {
                foreach (var root in comments.Where(c => c.SubCommentCount.GetValueOrDefault() > 0))
                {
                    await KuaishouGetSubCommentsAsync(client, db, photoId, root.CommentId, ct);
                }
            }

            pcursor = response.Data.VisionCommentList?.Pcursor ?? string.Empty;
            if (string.IsNullOrEmpty(pcursor) || pcursor.Equals("no_more", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }
    }

    /// <summary>
    /// 快手创作者信息与作品并存储。
    /// </summary>
    [CrawlerAction("creator", PlatformArgumentIndex = 0)]
    public static async Task CreatorAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("快手", ctx.Options.Platforms.Kuaishou)) return;

        var userId = ctx.RequirePositional(1, "创作者ID");
        var maxPages = ctx.GetIntOption(2, "max-pages");
        var client = CrawlerFactory.CreateKuaishouClient(ctx.Options.Platforms.Kuaishou.Cookies);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var profile = await client.ExecuteCreatorProfileAsync(new KuaishouCreatorProfileRequest
        {
            UserId = userId
        }, ct);

        if (profile.IsSuccessful() && profile.Data?.VisionProfile?.UserProfile is not null)
        {
            var creator = profile.Data.VisionProfile.UserProfile;
            NormalizeKuaishouCreator(creator);
            await db.Storageable(creator).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Kuaishou] 创作者信息已保存：{creator.Nickname} ({creator.UserId})");
        }
        else
        {
            Console.WriteLine("[Kuaishou] 获取创作者信息失败。");
        }

        var pcursor = string.Empty;
        var saved = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var videosResponse = await client.ExecuteCreatorVideosAsync(new KuaishouCreatorVideosRequest
            {
                UserId = userId,
                Pcursor = pcursor
            }, ct);

            if (!videosResponse.IsSuccessful() || videosResponse.Data?.VisionProfilePhotoList?.Feeds is not { Count: > 0 } feeds)
            {
                break;
            }

            var videos = feeds
                .Where(f => f.Photo is not null)
                .Select(f =>
                {
                    var video = f.Photo!;
                    NormalizeKuaishouVideo(video);
                    return video;
                })
                .ToList();

            saved += await db.Storageable(videos).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Kuaishou] 创作者作品第 {page} 页：获取 {videos.Count} 条，累计存储 {saved} 条");

            pcursor = videosResponse.Data.VisionProfilePhotoList?.Pcursor ?? string.Empty;
            if (string.IsNullOrEmpty(pcursor) || pcursor.Equals("no_more", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }
    }

    /// <summary>
    /// 快手首页流并存储。
    /// </summary>
    [CrawlerAction("homefeed", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task HomeFeedAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("快手", ctx.Options.Platforms.Kuaishou)) return;

        var count = ctx.GetIntOption(12, "count");
        var client = CrawlerFactory.CreateKuaishouClient(ctx.Options.Platforms.Kuaishou.Cookies);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var response = await client.ExecuteHomeFeedAsync(new KuaishouHomeFeedRequest
        {
            HotChannelId = "00"
        }, ct);

        if (!response.IsSuccessful() || response.Data?.BrilliantTypeData?.Feeds is not { Count: > 0 } feeds)
        {
            Console.WriteLine("[Kuaishou] 获取首页推荐失败。");
            return;
        }

        var videos = feeds
            .Where(f => f.Photo is not null)
            .Select(f =>
            {
                var video = f.Photo!;
                NormalizeKuaishouVideo(video);
                return video;
            })
            .ToList();

        var saved = await db.Storageable(videos).ExecuteCommandAsync(ct);
        Console.WriteLine($"[Kuaishou] 首页推荐：获取 {videos.Count} 条，存储 {saved} 条");
    }

    /// <summary>
    /// 快手登录状态检测。
    /// </summary>
    [CrawlerAction("login-check", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task<bool> LoginCheckAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("快手", ctx.Options.Platforms.Kuaishou)) return false;

        var client = CrawlerFactory.CreateKuaishouClient(ctx.Options.Platforms.Kuaishou.Cookies);
        var response = await client.ExecuteHomeFeedAsync(new KuaishouHomeFeedRequest
        {
            HotChannelId = "00"
        }, ctx.CancellationToken);

        var ok = response.IsSuccessful();
        Console.WriteLine($"[Login] 快手: {(ok ? "OK" : "FAIL")}");
        return ok;
    }

    private static async Task KuaishouGetSubCommentsAsync(
        KuaishouClient client,
        SqlSugarClient db,
        string photoId,
        string rootCommentId,
        CancellationToken ct)
    {
        var response = await client.ExecuteSubCommentAsync(new KuaishouSubCommentRequest
        {
            PhotoId = photoId,
            RootCommentId = rootCommentId
        }, ct);

        if (!response.IsSuccessful() || response.Data?.VisionSubCommentList?.SubComments is not { Count: > 0 } subComments)
        {
            return;
        }

        foreach (var sub in subComments)
        {
            NormalizeKuaishouComment(sub, photoId, rootCommentId);
        }

        await db.Storageable(subComments).ExecuteCommandAsync(ct);
    }

    private static void NormalizeKuaishouVideo(KuaishouVideo video)
    {
        video.CrawledAt = DateTimeOffset.Now;
        video.CoverUrl = video.CoverUrlRaw;

        if (video.Author is not null)
        {
            video.UserId = video.Author.Id;
            video.Nickname = video.Author.Name;
            video.Avatar = video.Author.HeaderUrl;
        }
    }

    private static void NormalizeKuaishouComment(KuaishouComment comment, string photoId, string? parentCommentId)
    {
        comment.PhotoId = photoId;
        comment.ParentCommentId = parentCommentId;
        comment.CrawledAt = DateTimeOffset.Now;

        if (comment.Author is not null)
        {
            comment.UserId = comment.Author.Id;
            comment.Nickname = comment.Author.Name;
            comment.Avatar = comment.Author.HeaderUrl;
        }
    }

    private static void NormalizeKuaishouCreator(KuaishouCreator creator)
    {
        creator.CrawledAt = DateTimeOffset.Now;
        if (creator.Profile is not null)
        {
            creator.Nickname = creator.Profile.UserName;
            creator.Avatar = creator.Profile.HeadUrl;
            creator.Description = creator.Profile.UserText;
            creator.Gender = creator.Profile.Gender;
        }
    }

}
