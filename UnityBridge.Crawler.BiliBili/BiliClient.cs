using UnityBridge.Crawler.Core;

namespace UnityBridge.Crawler.BiliBili;

/// <summary>
/// B站 API 客户端。
/// </summary>
public class BiliClient : CrawlerClientBase
{
    /// <summary>
    /// 获取当前客户端使用的配置项。
    /// </summary>
    public new BiliClientOptions ClientOptions { get; }

    protected override string PlatformName => "bilibili";

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
    /// 初始化B站客户端。
    /// </summary>
    public BiliClient(BiliClientOptions options, ISignClient signClient, AccountPoolManager? accountPool = null)
        : base(options, jsonOptions: CreateAotJsonOptions())
    {
        ClientOptions = options ?? throw new ArgumentNullException(nameof(options));
        _signClient = signClient ?? throw new ArgumentNullException(nameof(signClient));
        _accountPool = accountPool;

        FlurlClient.BaseUrl = options.Endpoint ?? BiliEndpoints.API;
        FlurlClient.WithTimeout(options.Timeout <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(options.Timeout));
    }

    private static JsonSerializerOptions CreateAotJsonOptions()
    {
        return new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = BiliJsonSerializerContext.Default
        };
    }

    /// <summary>
    /// 切换到下一个可用账号。
    /// </summary>
    public override async Task<bool> SwitchToNextAccountAsync(CancellationToken ct = default)
    {
        if (_accountPool is null) return false;

        _currentAccount = await _accountPool.GetAccountWithProxyAsync("bilibili", ct);
        return _currentAccount is not null;
    }

    /// <summary>
    /// 对请求参数进行 wbi 签名。
    /// </summary>
    public async Task<Dictionary<string, string>> SignParamsAsync(
        Dictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        // 调用签名服务获取 wts 和 w_rid
        // B站的 wbi 签名需要将所有参数排序后拼接，然后进行 MD5
        // 这里通过签名服务来处理
        var signResult = await _signClient.GetBilibiliSignAsync(new BilibiliSignRequest
        {
            ReqData = parameters,
            Cookies = Cookies
        }, ct);

        parameters["wts"] = signResult.Wts;
        parameters["w_rid"] = signResult.WRid;

        return parameters;
    }

    /// <summary>
    /// 创建带签名的 GET 请求 URL。
    /// </summary>
    public async Task<string> BuildSignedUrlAsync(
        string uri,
        Dictionary<string, string> parameters,
        bool enableSign = true,
        CancellationToken ct = default)
    {
        if (enableSign)
        {
            parameters = await SignParamsAsync(parameters, ct);
        }

        var queryString = string.Join("&", parameters.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{uri}?{queryString}";
    }

    /// <summary>
    /// 发送签名 GET 请求。
    /// </summary>
    public async Task<T> SendSignedGetAsync<T>(
        string uri,
        Dictionary<string, string> parameters,
        bool enableSign = true,
        CancellationToken ct = default)
        where T : BiliResponse, new()
    {
        var url = await BuildSignedUrlAsync(uri, parameters, enableSign, ct);

        IFlurlRequest flurlRequest = FlurlClient.Request(url)
            .WithHeader("Accept", "application/json, text/plain, */*")
            .WithHeader("Content-Type", "application/json;charset=UTF-8")
            .WithHeader("Cookie", Cookies)
            .WithHeader("Origin", ClientOptions.Origin)
            .WithHeader("Referer", ClientOptions.Referer)
            .WithHeader("User-Agent", ClientOptions.UserAgent);

        flurlRequest.Verb = HttpMethod.Get;

        using IFlurlResponse response = await base.SendFlurlRequestAsync(flurlRequest, null, ct);
        return await WrapFlurlResponseAsJsonAsync<T>(response, ct);
    }
}
