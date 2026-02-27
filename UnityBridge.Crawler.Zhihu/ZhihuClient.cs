using UnityBridge.Crawler.Core;

namespace UnityBridge.Crawler.Zhihu;

/// <summary>
/// 知乎 API 客户端。
/// </summary>
public class ZhihuClient : CrawlerClientBase
{
    /// <summary>
    /// 获取当前客户端使用的配置项。
    /// </summary>
    public new ZhihuClientOptions ClientOptions { get; }

    protected override string PlatformName => "zhihu";

    private readonly ISignClient _signClient;
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
    /// 初始化知乎客户端。
    /// </summary>
    public ZhihuClient(ZhihuClientOptions options, ISignClient signClient, AccountPoolManager? accountPool = null)
        : base(options, jsonOptions: CreateAotJsonOptions())
    {
        ClientOptions = options ?? throw new ArgumentNullException(nameof(options));
        _signClient = signClient ?? throw new ArgumentNullException(nameof(signClient));
        _accountPool = accountPool;

        FlurlClient.BaseUrl = options.Endpoint ?? ZhihuEndpoints.API;
        FlurlClient.WithTimeout(options.Timeout <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(options.Timeout));
    }

    private static JsonSerializerOptions CreateAotJsonOptions()
    {
        return new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = ZhihuJsonSerializerContext.Default
        };
    }

    /// <summary>
    /// 切换到下一个可用账号。
    /// </summary>
    public override async Task<bool> SwitchToNextAccountAsync(CancellationToken ct = default)
    {
        if (_accountPool is null) return false;

        _currentAccount = await _accountPool.GetAccountWithProxyAsync("zhihu", ct);
        return _currentAccount is not null;
    }

    /// <summary>
    /// 发送签名 GET 请求。
    /// </summary>
    public async Task<T> SendSignedGetAsync<T>(
        string uri,
        Dictionary<string, string>? parameters = null,
        CancellationToken ct = default)
        where T : ZhihuResponse, new()
    {
        var url = uri;
        if (parameters is { Count: > 0 })
        {
            var queryString = string.Join("&", parameters.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            url = $"{uri}?{queryString}";
        }

        // 调用签名服务获取 x-zse-96 和 x-zst-81
        var signResult = await _signClient.GetZhihuSignAsync(new ZhihuSignRequest
        {
            Uri = url,
            Cookies = Cookies
        }, ct);

        IFlurlRequest flurlRequest = FlurlClient.Request(url)
            .WithHeader("Accept", "*/*")
            .WithHeader("Accept-Language", "zh-CN,zh;q=0.9")
            .WithHeader("Cookie", Cookies)
            .WithHeader("Referer", ClientOptions.Referer)
            .WithHeader("User-Agent", ClientOptions.UserAgent)
            .WithHeader("x-api-version", "3.0.91")
            .WithHeader("x-app-za", "OS=Web")
            .WithHeader("x-requested-with", "fetch")
            .WithHeader("x-zse-93", "101_3_3.0")
            .WithHeader("x-zse-96", signResult.XZse96)
            .WithHeader("x-zst-81", signResult.XZst81);

        flurlRequest.Verb = HttpMethod.Get;

        using IFlurlResponse response = await base.SendFlurlRequestAsync(flurlRequest, null, ct);
        return await WrapFlurlResponseAsJsonAsync<T>(response, ct);
    }

    /// <summary>
    /// 发送 GET 请求获取 HTML（用于详情页解析）。
    /// </summary>
    public async Task<string> SendGetHtmlAsync(
        string uri,
        string? baseUrl = null,
        CancellationToken ct = default)
    {
        var signResult = await _signClient.GetZhihuSignAsync(new ZhihuSignRequest
        {
            Uri = uri,
            Cookies = Cookies
        }, ct);

        var url = (baseUrl ?? ZhihuEndpoints.API) + uri;

        IFlurlRequest flurlRequest = new FlurlClient().Request(url)
            .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
            .WithHeader("Cookie", Cookies)
            .WithHeader("Referer", ClientOptions.Referer)
            .WithHeader("User-Agent", ClientOptions.UserAgent)
            .WithHeader("x-zse-96", signResult.XZse96)
            .WithHeader("x-zst-81", signResult.XZst81);

        flurlRequest.Verb = HttpMethod.Get;

        using IFlurlResponse response = await base.SendFlurlRequestAsync(flurlRequest, null, ct);
        return await response.ResponseMessage.Content.ReadAsStringAsync(ct);
    }
}
