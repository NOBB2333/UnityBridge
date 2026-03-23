using SqlSugar;

namespace UnityBridge.Crawler;

public sealed class CrawlerCommandContext
{
    public string ActionName { get; }
    public ParsedArgs Args { get; }
    public CrawlerOptions Options { get; }
    public SqlSugarClient Db { get; }
    public CancellationToken CancellationToken { get; }
    public string? Platform { get; }
    public string? PlatformDisplayName { get; }

    public IReadOnlyList<string> Positionals => Args.Positionals;
    public IReadOnlyDictionary<string, string> OptionMap => Args.Options;

    public CrawlerCommandContext(
        string actionName,
        ParsedArgs args,
        CrawlerOptions options,
        SqlSugarClient db,
        CancellationToken cancellationToken,
        string? platform = null,
        string? platformDisplayName = null)
    {
        ActionName = actionName;
        Args = args;
        Options = options;
        Db = db;
        CancellationToken = cancellationToken;
        Platform = platform;
        PlatformDisplayName = platformDisplayName;
    }

    public CrawlerCommandContext WithPlatform(string platform, string platformDisplayName)
        => new(ActionName, Args, Options, Db, CancellationToken, platform, platformDisplayName);

    public string? GetOption(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Args.Options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public int GetIntOption(int fallback, params string[] keys)
    {
        var raw = GetOption(keys);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }

    public bool GetBoolOption(bool fallback, params string[] keys)
    {
        var raw = GetOption(keys);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (bool.TryParse(raw, out var value))
        {
            return value;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "1" or "yes" or "on" => true,
            "0" or "no" or "off" => false,
            _ => fallback
        };
    }

    public string RequirePositional(int index, string displayName)
    {
        if (index < Args.Positionals.Count && !string.IsNullOrWhiteSpace(Args.Positionals[index]))
        {
            return Args.Positionals[index];
        }

        throw new InvalidOperationException($"[错误] 缺少参数：{displayName}");
    }

    public bool EnsureReady(string platformName, PlatformConfig config)
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

    public bool EnsureReadyOrSkip(string platformName, PlatformConfig config)
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
}

public sealed record ParsedArgs(
    List<string> Positionals,
    Dictionary<string, string> Options
);
