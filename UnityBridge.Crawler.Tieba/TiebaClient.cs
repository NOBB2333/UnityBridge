using UnityBridge.Crawler.Core;

namespace UnityBridge.Crawler.Tieba;

/// <summary>
/// 百度贴吧客户端。
/// </summary>
public class TiebaClient : CrawlerClientBase
{
    /// <summary>
    /// 获取当前客户端使用的配置项。
    /// </summary>
    public new TiebaClientOptions ClientOptions { get; }

    protected override string PlatformName => "tieba";

    private readonly AccountPoolManager? _accountPool;
    private AccountInfo? _currentAccount;

    /// <summary>
    /// 获取或设置当前使用的 Cookies。
    /// </summary>
    public string Cookies
    {
        get => _currentAccount?.Cookies ?? ClientOptions.Cookies ?? string.Empty;
        set
        {
            if (_currentAccount is not null)
                _currentAccount.Cookies = value;
            else
                ClientOptions.Cookies = value;
        }
    }

    /// <summary>
    /// 初始化贴吧客户端。
    /// </summary>
    public TiebaClient(TiebaClientOptions options, AccountPoolManager? accountPool = null)
        : base(options, jsonOptions: CreateAotJsonOptions())
    {
        ClientOptions = options ?? throw new ArgumentNullException(nameof(options));
        _accountPool = accountPool;

        FlurlClient.BaseUrl = options.Endpoint ?? TiebaEndpoints.WEB;
        FlurlClient.WithTimeout(options.Timeout <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(options.Timeout));
    }

    private static JsonSerializerOptions CreateAotJsonOptions()
    {
        return new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = TiebaJsonSerializerContext.Default
        };
    }

    /// <summary>
    /// 切换到下一个可用账号。
    /// </summary>
    public override async Task<bool> SwitchToNextAccountAsync(CancellationToken ct = default)
    {
        if (_accountPool is null) return false;

        _currentAccount = await _accountPool.GetAccountWithProxyAsync("tieba", ct);
        return _currentAccount is not null;
    }

    /// <summary>
    /// 发送 GET 请求获取 HTML。
    /// </summary>
    public async Task<string> SendGetHtmlAsync(
        string uri,
        Dictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        var url = uri;
        if (parameters is { Count: > 0 })
        {
            var queryString = string.Join("&", parameters.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            url = $"{uri}?{queryString}";
        }

        IFlurlRequest flurlRequest = FlurlClient.Request(url)
            .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
            .WithHeader("Cookie", Cookies)
            .WithHeader("Origin", ClientOptions.Origin)
            .WithHeader("Referer", ClientOptions.Referer)
            .WithHeader("User-Agent", ClientOptions.UserAgent);

        flurlRequest.Verb = HttpMethod.Get;

        using IFlurlResponse response = await base.SendFlurlRequestAsync(flurlRequest, null, ct);
        return await response.ResponseMessage.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// 发送 GET 请求获取 JSON。
    /// </summary>
    public async Task<T> SendGetJsonAsync<T>(
        string uri,
        Dictionary<string, string>? parameters = null,
        CancellationToken ct = default)
        where T : TiebaResponse, new()
    {
        var url = uri;
        if (parameters is { Count: > 0 })
        {
            var queryString = string.Join("&", parameters.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            url = $"{uri}?{queryString}";
        }

        IFlurlRequest flurlRequest = FlurlClient.Request(url)
            .WithHeader("Accept", "application/json, text/plain, */*")
            .WithHeader("Cookie", Cookies)
            .WithHeader("Origin", ClientOptions.Origin)
            .WithHeader("Referer", ClientOptions.Referer)
            .WithHeader("User-Agent", ClientOptions.UserAgent);

        flurlRequest.Verb = HttpMethod.Get;

        using IFlurlResponse response = await base.SendFlurlRequestAsync(flurlRequest, null, ct);
        return await WrapFlurlResponseAsJsonAsync<T>(response, ct);
    }
}
