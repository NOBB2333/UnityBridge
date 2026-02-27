using UnityBridge.Crawler.Core;

namespace UnityBridge.Crawler.Douyin;

/// <summary>
/// 抖音 API 客户端。
/// </summary>
public class DouyinClient : CrawlerClientBase
{
    /// <summary>
    /// 获取当前客户端使用的配置项。
    /// </summary>
    public new DouyinClientOptions ClientOptions { get; }

    protected override string PlatformName => "douyin";

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
    /// 初始化抖音客户端。
    /// </summary>
    public DouyinClient(DouyinClientOptions options, ISignClient signClient, AccountPoolManager? accountPool = null)
        : base(options, jsonOptions: CreateAotJsonOptions())
    {
        ClientOptions = options ?? throw new ArgumentNullException(nameof(options));
        _signClient = signClient ?? throw new ArgumentNullException(nameof(signClient));
        _accountPool = accountPool;

        FlurlClient.BaseUrl = options.Endpoint ?? DouyinEndpoints.API;
        FlurlClient.WithTimeout(options.Timeout <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(options.Timeout));
    }

    private static JsonSerializerOptions CreateAotJsonOptions()
    {
        return new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = DouyinJsonSerializerContext.Default
        };
    }

    /// <summary>
    /// 切换到下一个可用账号。
    /// </summary>
    public override async Task<bool> SwitchToNextAccountAsync(CancellationToken ct = default)
    {
        if (_accountPool is null) return false;

        _currentAccount = await _accountPool.GetAccountWithProxyAsync("douyin", ct);
        return _currentAccount is not null;
    }

    /// <summary>
    /// 获取通用请求参数。
    /// </summary>
    private Dictionary<string, string> GetCommonParams() => new()
    {
        ["device_platform"] = "webapp",
        ["aid"] = "6383",
        ["channel"] = "channel_pc_web",
        ["publish_video_strategy_type"] = "2",
        ["update_version_code"] = "170400",
        ["pc_client_type"] = "1",
        ["version_code"] = "170400",
        ["version_name"] = "17.4.0",
        ["cookie_enabled"] = "true",
        ["screen_width"] = "2560",
        ["screen_height"] = "1440",
        ["browser_language"] = "zh-CN",
        ["browser_platform"] = "MacIntel",
        ["browser_name"] = "Chrome",
        ["browser_version"] = "135.0.0.0",
        ["browser_online"] = "true",
        ["engine_name"] = "Blink",
        ["engine_version"] = "135.0.0.0",
        ["os_name"] = "Mac OS",
        ["os_version"] = "10.15.7",
        ["cpu_core_num"] = "8",
        ["device_memory"] = "8",
        ["platform"] = "PC",
        ["downlink"] = "4.45",
        ["effective_type"] = "4g",
        ["round_trip_time"] = "100"
    };

    /// <summary>
    /// 获取验证参数。
    /// </summary>
    private Dictionary<string, string> GetVerifyParams() => new()
    {
        ["webid"] = ClientOptions.WebId ?? "",
        ["msToken"] = ClientOptions.MsToken ?? "",
        ["verifyFp"] = ClientOptions.VerifyFp ?? "",
        ["fp"] = ClientOptions.VerifyFp ?? ""
    };

    /// <summary>
    /// 对请求参数进行签名（获取 a_bogus）。
    /// </summary>
    public async Task<Dictionary<string, string>> SignParamsAsync(
        string uri,
        Dictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        // 合并通用参数和验证参数
        foreach (var kv in GetCommonParams())
        {
            if (!parameters.ContainsKey(kv.Key))
                parameters[kv.Key] = kv.Value;
        }

        foreach (var kv in GetVerifyParams())
        {
            if (!parameters.ContainsKey(kv.Key))
                parameters[kv.Key] = kv.Value;
        }

        // 调用签名服务获取 a_bogus
        var queryParams = string.Join("&", parameters.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var signResult = await _signClient.GetDouyinSignAsync(new DouyinSignRequest
        {
            Uri = uri,
            Cookies = Cookies
        }, ct);

        parameters["a_bogus"] = signResult.ABogus;

        return parameters;
    }

    /// <summary>
    /// 发送签名 GET 请求。
    /// </summary>
    public async Task<T> SendSignedGetAsync<T>(
        string uri,
        Dictionary<string, string> parameters,
        bool enableSign = true,
        CancellationToken ct = default)
        where T : DouyinResponse, new()
    {
        if (enableSign)
        {
            parameters = await SignParamsAsync(uri, parameters, ct);
        }

        var queryString = string.Join("&", parameters.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var url = $"{uri}?{queryString}";

        IFlurlRequest flurlRequest = FlurlClient.Request(url)
            .WithHeader("Accept", "application/json, text/plain, */*")
            .WithHeader("Content-Type", "application/json")
            .WithHeader("Accept-Language", "zh-CN,zh;q=0.9")
            .WithHeader("Cookie", Cookies)
            .WithHeader("Origin", ClientOptions.Origin)
            .WithHeader("Referer", ClientOptions.Referer)
            .WithHeader("User-Agent", ClientOptions.UserAgent);

        flurlRequest.Verb = HttpMethod.Get;

        using IFlurlResponse response = await base.SendFlurlRequestAsync(flurlRequest, null, ct);
        return await WrapFlurlResponseAsJsonAsync<T>(response, ct);
    }
}
