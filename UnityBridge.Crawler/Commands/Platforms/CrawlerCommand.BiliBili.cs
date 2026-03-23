using SqlSugar;
using UnityBridge.Crawler.BiliBili;
using UnityBridge.Crawler.BiliBili.Extensions;
using UnityBridge.Crawler.BiliBili.Models;

namespace UnityBridge.Crawler;

[CrawlerPlatform("bili", "B站", "bilibili", "b站", "哔哩哔哩")]
public static class BiliCli
{
    /// <summary>
    /// B站关键词搜索并存储。
    /// </summary>
    [CrawlerAction("search", PlatformArgumentIndex = 1, PlatformOptional = true, SupportsAllPlatforms = true, RunInParallelForAll = true)]
    public static async Task SearchAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("B站", ctx.Options.Platforms.BiliBili)) return;

        var keyword = ctx.RequirePositional(0, "搜索关键词");
        var maxPages = ctx.GetIntOption(ctx.Options.MaxPages, "max-pages");
        var delayMinMs = ctx.Options.DefaultDelay.MinMs;
        var delayMaxMs = ctx.Options.DefaultDelay.MaxMs;
        var ct = ctx.CancellationToken;
        var client = CrawlerFactory.CreateBiliClient(ctx.Options.Platforms.BiliBili.Cookies, ctx.Options.SignServerUrl);
        var db = ctx.Db;

        Console.WriteLine($"[Bili] 开始搜索关键词：{keyword}");

        for (int page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            try
            {
                var request = new BiliVideoSearchRequest
                {
                    Keyword = keyword,
                    Page = page,
                    PageSize = 20
                };

                var response = await client.ExecuteVideoSearchAsync(request, ct);

                if (!response.IsSuccessful() || response.Data?.Result is not { Count: > 0 } results)
                {
                    Console.WriteLine("[Bili] 没有更多结果。");
                    break;
                }

                var videos = results.Select(v => new BiliVideo
                {
                    Aid = v.Aid,
                    Bvid = v.Bvid,
                    VideoType = "video",
                    Title = v.Title,
                    Description = v.Description,
                    Duration = ParseBiliDurationToSeconds(v.Duration),
                    CoverUrl = v.Pic,
                    Nickname = v.Author,
                    UserId = v.Mid,
                    ViewCount = v.Play,
                    DanmakuCount = v.Danmaku,
                    PubDate = v.Pubdate,
                    Keyword = keyword,
                    CrawledAt = DateTimeOffset.Now
                }).ToList();
                videos.ForEach(EnsureBiliVideoStorageSafe);

                var count = await db.Storageable(videos).ExecuteCommandAsync(ct);
                Console.WriteLine($"[Bili] 第 {page} 页：获取 {results.Count} 条，存储 {count} 条视频");

                await Task.Delay(Random.Shared.Next(delayMinMs, delayMaxMs), ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Bili] 搜索已取消。");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bili] 搜索异常：{ex.Message}");
            }
        }

        Console.WriteLine($"[Bili] 搜索完成：{keyword}");
    }

    /// <summary>
    /// B站视频详情并存储。
    /// </summary>
    [CrawlerAction("detail", PlatformArgumentIndex = 0)]
    public static async Task<BiliVideo?> DetailAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("B站", ctx.Options.Platforms.BiliBili)) return null;

        var contentId = ctx.RequirePositional(1, "内容ID");
        var aid = ctx.GetOption("aid");
        var bvid = ctx.GetOption("bvid");
        if (string.IsNullOrEmpty(aid) && string.IsNullOrEmpty(bvid))
        {
            if (contentId.StartsWith("BV", StringComparison.OrdinalIgnoreCase))
                bvid = contentId;
            else
                aid = contentId;
        }

        var client = CrawlerFactory.CreateBiliClient(ctx.Options.Platforms.BiliBili.Cookies, ctx.Options.SignServerUrl);
        var response = await client.ExecuteVideoDetailAsync(new BiliVideoDetailRequest
        {
            Aid = aid,
            Bvid = bvid
        }, ctx.CancellationToken);

        if (!response.IsSuccessful() || response.Data?.View is null)
        {
            Console.WriteLine("[Bili] 获取视频详情失败。");
            return null;
        }

        var video = response.Data.View;
        NormalizeBiliVideo(video);
        await ctx.Db.Storageable(video).ExecuteCommandAsync(ctx.CancellationToken);

        Console.WriteLine($"[Bili] 详情成功：{video.Bvid ?? video.Aid.ToString()} {video.Title}");
        return video;
    }

    /// <summary>
    /// B站评论并存储。
    /// </summary>
    [CrawlerAction("comments", PlatformArgumentIndex = 0)]
    public static async Task CommentsAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("B站", ctx.Options.Platforms.BiliBili)) return;

        var videoId = ctx.RequirePositional(1, "内容ID");
        var maxPages = ctx.GetIntOption(3, "max-pages");
        var includeSubComments = ctx.GetBoolOption(true, "include-sub");
        var client = CrawlerFactory.CreateBiliClient(ctx.Options.Platforms.BiliBili.Cookies, ctx.Options.SignServerUrl);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var next = 0;
        var total = 0;

        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var response = await client.ExecuteCommentPageAsync(new BiliCommentRequest
            {
                VideoId = videoId,
                Next = next
            }, ct);

            if (!response.IsSuccessful() || response.Data?.Replies is not { Count: > 0 } comments)
            {
                break;
            }

            foreach (var comment in comments)
            {
                NormalizeBiliComment(comment, videoId, null);
            }

            total += await db.Storageable(comments).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Bili] 评论第 {page} 页：获取 {comments.Count} 条，累计存储 {total} 条");

            if (includeSubComments)
            {
                foreach (var root in comments.Where(c => c.ReplyCount.GetValueOrDefault() > 0))
                {
                    await BiliGetSubCommentsAsync(client, db, videoId, root.Rpid.ToString(), ct);
                }
            }

            if (response.Data.Cursor?.IsEnd == true)
            {
                break;
            }

            next = response.Data.Cursor?.Next ?? 0;
            await Task.Delay(Random.Shared.Next(400, 1000), ct);
        }
    }

    /// <summary>
    /// B站创作者信息与内容并存储。
    /// </summary>
    [CrawlerAction("creator", PlatformArgumentIndex = 0)]
    public static async Task CreatorAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReady("B站", ctx.Options.Platforms.BiliBili)) return;

        var mid = ctx.RequirePositional(1, "创作者ID");
        var maxPages = ctx.GetIntOption(2, "max-pages");
        var client = CrawlerFactory.CreateBiliClient(ctx.Options.Platforms.BiliBili.Cookies, ctx.Options.SignServerUrl);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var profile = await client.ExecuteUpInfoAsync(new BiliUpInfoRequest
        {
            Mid = mid
        }, ct);

        if (profile.IsSuccessful() && profile.Data is not null)
        {
            var creator = profile.Data;
            creator.CrawledAt = DateTimeOffset.Now;
            await db.Storageable(creator).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Bili] 创作者信息已保存：{creator.Nickname} ({creator.Mid})");
        }
        else
        {
            Console.WriteLine("[Bili] 获取创作者信息失败。");
        }

        var savedVideos = 0;
        for (var page = 1; page <= maxPages && !ct.IsCancellationRequested; page++)
        {
            var videosResponse = await client.ExecuteUpVideosAsync(new BiliUpVideosRequest
            {
                Mid = mid,
                Pn = page,
                Ps = 30
            }, ct);

            if (!videosResponse.IsSuccessful() || videosResponse.Data?.List?.Vlist is not { Count: > 0 } list)
            {
                break;
            }

            var videos = list.Select(v => new BiliVideo
            {
                Aid = v.Aid,
                Bvid = v.Bvid,
                VideoType = "video",
                Title = v.Title,
                Description = v.Description,
                CoverUrl = v.Pic,
                ViewCount = v.Play,
                ReplyCount = v.Comment,
                CreateTime = v.Created,
                UserId = long.TryParse(mid, out var uid) ? uid : null,
                CrawledAt = DateTimeOffset.Now
            }).ToList();
            videos.ForEach(EnsureBiliVideoStorageSafe);

            savedVideos += await db.Storageable(videos).ExecuteCommandAsync(ct);
            Console.WriteLine($"[Bili] 创作者视频第 {page} 页：获取 {list.Count} 条，累计存储 {savedVideos} 条");

            if (videosResponse.Data.Page is not { } pageInfo || pageInfo.Pn * pageInfo.Ps >= pageInfo.Count)
            {
                break;
            }
        }
    }

    /// <summary>
    /// B站首页推荐并存储。
    /// </summary>
    [CrawlerAction("homefeed", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task HomeFeedAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("B站", ctx.Options.Platforms.BiliBili)) return;

        var pageCount = ctx.GetIntOption(12, "count");
        var client = CrawlerFactory.CreateBiliClient(ctx.Options.Platforms.BiliBili.Cookies, ctx.Options.SignServerUrl);
        var db = ctx.Db;
        var ct = ctx.CancellationToken;
        var response = await client.ExecuteHomeFeedAsync(new BiliHomeFeedRequest
        {
            PageCount = pageCount
        }, ct);

        if (!response.IsSuccessful() || response.Data?.Items is not { Count: > 0 } items)
        {
            Console.WriteLine("[Bili] 获取首页推荐失败。");
            return;
        }

        var videos = items.Select(i => new BiliVideo
        {
            Aid = i.Id,
            Bvid = i.Bvid,
            VideoType = "video",
            Title = i.Title,
            CoverUrl = i.Pic,
            Duration = i.Duration,
            UserId = i.Owner?.Mid,
            Nickname = i.Owner?.Name,
            ViewCount = i.Stat?.View,
            DanmakuCount = i.Stat?.Danmaku,
            ReplyCount = i.Stat?.Reply,
            LikeCount = i.Stat?.Like,
            CoinCount = i.Stat?.Coin,
            FavoriteCount = i.Stat?.Favorite,
            ShareCount = i.Stat?.Share,
            CrawledAt = DateTimeOffset.Now
        }).ToList();
        videos.ForEach(EnsureBiliVideoStorageSafe);

        var saved = await db.Storageable(videos).ExecuteCommandAsync(ct);
        Console.WriteLine($"[Bili] 首页推荐：获取 {items.Count} 条，存储 {saved} 条");
    }

    /// <summary>
    /// B站登录状态检测。
    /// </summary>
    [CrawlerAction("login-check", PlatformArgumentIndex = 0, PlatformOptional = true, SupportsAllPlatforms = true)]
    public static async Task<bool> LoginCheckAsync(CrawlerCommandContext ctx)
    {
        if (!ctx.EnsureReadyOrSkip("B站", ctx.Options.Platforms.BiliBili)) return false;

        var client = CrawlerFactory.CreateBiliClient(ctx.Options.Platforms.BiliBili.Cookies, ctx.Options.SignServerUrl);
        var response = await client.ExecuteHomeFeedAsync(new BiliHomeFeedRequest
        {
            PageCount = 1
        }, ctx.CancellationToken);

        var ok = response.IsSuccessful();
        Console.WriteLine($"[Login] B站: {(ok ? "OK" : "FAIL")}");
        return ok;
    }

    /// <summary>
    /// B站创建二维码登录会话。
    /// </summary>
    public static async Task<BiliQrLoginSession?> BiliCreateQrLoginSessionAsync(
        BiliClient client,
        CancellationToken ct = default)
    {
        var response = await client.ExecuteQrCodeGenerateAsync(new BiliQrCodeGenerateRequest(), ct);
        if (!response.IsSuccessful() || response.Data is null ||
            string.IsNullOrWhiteSpace(response.Data.Url) ||
            string.IsNullOrWhiteSpace(response.Data.QrcodeKey))
        {
            Console.WriteLine("[Bili] 生成二维码失败。");
            return null;
        }

        return new BiliQrLoginSession(response.Data.QrcodeKey, response.Data.Url);
    }

    /// <summary>
    /// B站轮询二维码登录状态，成功后返回 Cookies。
    /// </summary>
    public static async Task<string?> BiliWaitForQrLoginAsync(
        BiliClient client,
        string qrcodeKey,
        int timeoutSeconds = 120,
        int pollIntervalSeconds = 1,
        CancellationToken ct = default)
    {
        var startAt = DateTimeOffset.UtcNow;
        var hasScanned = false;

        while (!ct.IsCancellationRequested && DateTimeOffset.UtcNow - startAt < TimeSpan.FromSeconds(timeoutSeconds))
        {
            var response = await client.ExecuteQrCodePollAsync(new BiliQrCodePollRequest
            {
                QrcodeKey = qrcodeKey
            }, ct);

            if (!response.IsSuccessful() || response.Data is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), ct);
                continue;
            }

            var code = response.Data.Code;
            switch (code)
            {
                case 86101:
                    break;

                case 86090:
                    if (!hasScanned)
                    {
                        Console.WriteLine("[Bili] 已扫码，请在手机上确认登录...");
                        hasScanned = true;
                    }
                    break;

                case 86038:
                    Console.WriteLine("[Bili] 二维码已过期，请重新执行 login。");
                    return null;

                case 0:
                {
                    var fromUrl = ExtractCookiesFromCallbackUrl(response.Data.Url);
                    var fromManager = MergeCookieStrings(
                        client.GetCookies("passport.bilibili.com"),
                        client.GetCookies("www.bilibili.com"),
                        client.GetCookies("api.bilibili.com"));
                    var finalCookies = MergeCookieStrings(fromUrl, fromManager);

                    if (string.IsNullOrWhiteSpace(finalCookies))
                    {
                        Console.WriteLine("[Bili] 登录已确认，但未解析到 Cookies。");
                        return null;
                    }

                    client.Cookies = finalCookies;
                    return finalCookies;
                }

                default:
                    Console.WriteLine($"[Bili] 登录状态：{code} {response.Data.Message}");
                    break;
            }

            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), ct);
        }

        Console.WriteLine("[Bili] 登录超时，请重试。");
        return null;
    }

    public sealed record BiliQrLoginSession(string QrcodeKey, string LoginUrl);

    private static async Task BiliGetSubCommentsAsync(
        BiliClient client,
        SqlSugarClient db,
        string videoId,
        string rootCommentId,
        CancellationToken ct)
    {
        var response = await client.ExecuteSubCommentAsync(new BiliSubCommentRequest
        {
            VideoId = videoId,
            RootCommentId = rootCommentId,
            Pn = 1,
            Ps = 20
        }, ct);

        if (!response.IsSuccessful() || response.Data?.Replies is not { Count: > 0 } subComments)
        {
            return;
        }

        foreach (var sub in subComments)
        {
            NormalizeBiliComment(sub, videoId, rootCommentId);
        }

        await db.Storageable(subComments).ExecuteCommandAsync(ct);
    }

    private static void NormalizeBiliVideo(BiliVideo video)
    {
        video.CrawledAt = DateTimeOffset.Now;

        if (video.Owner is not null)
        {
            video.UserId = video.Owner.Mid;
            video.Nickname = video.Owner.Name;
            video.Avatar = video.Owner.Face;
        }

        if (video.Stat is not null)
        {
            video.ViewCount = video.Stat.View;
            video.DanmakuCount = video.Stat.Danmaku;
            video.ReplyCount = video.Stat.Reply;
            video.FavoriteCount = video.Stat.Favorite;
            video.CoinCount = video.Stat.Coin;
            video.ShareCount = video.Stat.Share;
            video.LikeCount = video.Stat.Like;
        }

        EnsureBiliVideoStorageSafe(video);
    }

    private static void EnsureBiliVideoStorageSafe(BiliVideo video)
    {
        video.Bvid ??= string.Empty;
        video.VideoType = string.IsNullOrWhiteSpace(video.VideoType) ? "video" : video.VideoType;
        video.Title ??= string.Empty;
        video.Description ??= string.Empty;

        video.CreateTime ??= video.PubDate ?? 0;
        video.PubDate ??= video.CreateTime ?? 0;
        video.Duration ??= 0;

        video.ViewCount ??= 0;
        video.DanmakuCount ??= 0;
        video.LikeCount ??= 0;
        video.CoinCount ??= 0;
        video.FavoriteCount ??= 0;
        video.ShareCount ??= 0;
        video.ReplyCount ??= 0;

        video.CoverUrl ??= string.Empty;
        video.UserId ??= 0;
        video.Nickname ??= string.Empty;
        video.Avatar ??= string.Empty;

        if (string.IsNullOrWhiteSpace(video.VideoUrl))
        {
            video.VideoUrl = !string.IsNullOrWhiteSpace(video.Bvid)
                ? $"https://www.bilibili.com/video/{video.Bvid}"
                : string.Empty;
        }

        video.Keyword ??= string.Empty;
        video.CrawledAt ??= DateTimeOffset.Now;
    }

    private static int ParseBiliDurationToSeconds(string? durationText)
    {
        if (string.IsNullOrWhiteSpace(durationText))
        {
            return 0;
        }

        var parts = durationText.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var mm) &&
            int.TryParse(parts[1], out var ss))
        {
            return Math.Max(0, mm * 60 + ss);
        }

        if (parts.Length == 3 &&
            int.TryParse(parts[0], out var hh) &&
            int.TryParse(parts[1], out mm) &&
            int.TryParse(parts[2], out ss))
        {
            return Math.Max(0, hh * 3600 + mm * 60 + ss);
        }

        return 0;
    }

    private static void NormalizeBiliComment(BiliComment comment, string videoId, string? parentId)
    {
        comment.VideoId = videoId;
        comment.ParentRpid = string.IsNullOrEmpty(parentId) ? null : long.TryParse(parentId, out var parentRpid) ? parentRpid : null;
        comment.CrawledAt = DateTimeOffset.Now;

        if (comment.ContentObj is not null)
        {
            comment.Content = comment.ContentObj.Message;
        }

        if (comment.Member is not null)
        {
            comment.UserId = long.TryParse(comment.Member.Mid, out var uid) ? uid : null;
            comment.Nickname = comment.Member.Uname;
            comment.Avatar = comment.Member.Avatar;
        }
    }

    private static string ExtractCookiesFromCallbackUrl(string? callbackUrl)
    {
        if (string.IsNullOrWhiteSpace(callbackUrl) ||
            !Uri.TryCreate(callbackUrl, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        var queryMap = ParseQueryString(uri.Query);
        var allowedKeys = new[]
        {
            "SESSDATA", "bili_jct", "DedeUserID", "DedeUserID__ckMd5", "sid"
        };

        var pairs = new List<string>();
        foreach (var key in allowedKeys)
        {
            if (queryMap.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                pairs.Add($"{key}={value}");
            }
        }

        return string.Join("; ", pairs);
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
        {
            return map;
        }

        var raw = query.TrimStart('?');
        foreach (var pair in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..idx]);
            var value = Uri.UnescapeDataString(pair[(idx + 1)..]);
            map[key] = value;
        }

        return map;
    }

    private static string MergeCookieStrings(params string?[] cookieStrings)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in cookieStrings)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            foreach (var token in source.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var part = token.Trim();
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                var idx = part.IndexOf('=');
                if (idx <= 0)
                {
                    continue;
                }

                var key = part[..idx].Trim();
                var value = part[(idx + 1)..].Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    merged[key] = value;
                }
            }
        }

        return string.Join("; ", merged.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    [CrawlerAction("login", PlatformArgumentIndex = 0)]
    public static async Task LoginAsync(CrawlerCommandContext ctx)
    {
        var method = (ctx.GetOption("method") ?? "qr").Trim().ToLowerInvariant();
        if (method != "qr")
        {
            Console.WriteLine($"[错误] B站当前仅支持 --method qr，收到: {method}");
            return;
        }

        var timeout = ctx.GetIntOption(120, "timeout");
        var pollInterval = ctx.GetIntOption(1, "poll-interval");
        var writeConfig = ctx.GetBoolOption(true, "write-config");
        var client = CrawlerFactory.CreateBiliClient(ctx.Options.Platforms.BiliBili.Cookies, ctx.Options.SignServerUrl);

        var session = await BiliCreateQrLoginSessionAsync(client, ctx.CancellationToken);
        if (session is null)
        {
            Console.WriteLine("[Login] 生成二维码失败。");
            return;
        }

        Console.WriteLine("[Login] 请使用哔哩哔哩 APP 扫描下方二维码：");
        CrawlerCommandQrRender.RenderQrCodeToConsole(session.LoginUrl);
        Console.WriteLine($"[Login] 若终端二维码显示异常，可打开此链接：{session.LoginUrl}");

        var cookies = await BiliWaitForQrLoginAsync(
            client,
            session.QrcodeKey,
            timeout,
            pollInterval,
            ctx.CancellationToken);

        if (string.IsNullOrWhiteSpace(cookies))
        {
            Console.WriteLine("[Login] 登录失败。");
            return;
        }

        ctx.Options.Platforms.BiliBili.Cookies = cookies;
        Console.WriteLine("[Login] 登录成功，B站 Cookies 已写入内存。");

        if (!writeConfig)
        {
            return;
        }

        if (CrawlerCommandQrRender.TryUpdateCookiesInConfig("BiliBili", cookies, out var path, out var error))
        {
            Console.WriteLine($"[Login] 已更新配置文件: {path}");
        }
        else
        {
            Console.WriteLine($"[Login] 配置写入失败: {error}");
        }
    }
}
