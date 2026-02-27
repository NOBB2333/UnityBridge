using UnityBridge.Crawler.Core;

namespace UnityBridge.Crawler.XiaoHongShu;

/// <summary>
/// 小红书 API 客户端。
/// </summary>
public class XhsClient : CrawlerClientBase
{
    /// <summary>
    /// 获取当前客户端使用的配置项。
    /// </summary>
    public new XhsClientOptions ClientOptions { get; }

    protected override string PlatformName => "xhs";


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
    /// 初始化小红书客户端。
    /// </summary>
    /// <param name="options">客户端配置项。</param>
    /// <param name="signClient">签名服务客户端。</param>
    /// <param name="accountPool">账号池管理器（可选）。</param>
    public XhsClient(XhsClientOptions options, ISignClient signClient, AccountPoolManager? accountPool = null)
        : base(options, jsonOptions: CreateAotJsonOptions())
    {
        ClientOptions = options ?? throw new ArgumentNullException(nameof(options));
        _signClient = signClient ?? throw new ArgumentNullException(nameof(signClient));
        _accountPool = accountPool;

        FlurlClient.BaseUrl = options.Endpoint ?? XhsEndpoints.API;
        FlurlClient.WithTimeout(options.Timeout <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(options.Timeout));
    }

    private static JsonSerializerOptions CreateAotJsonOptions()
    {
        return new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = XhsJsonSerializerContext.Default
        };
    }

    /// <summary>
    /// 切换到下一个可用账号。
    /// </summary>
    public override async Task<bool> SwitchToNextAccountAsync(CancellationToken ct = default)
    {
        if (_accountPool is null) return false;

        _currentAccount = await _accountPool.GetAccountWithProxyAsync("xhs", ct);
        return _currentAccount is not null;
    }

    /// <summary>
    /// 标记当前账号无效并切换。
    /// </summary>
    public async Task MarkCurrentAccountInvalidAsync(CancellationToken ct = default)
    {
        if (_accountPool is null || _currentAccount is null) return;

        await _accountPool.MarkAccountInvalidAsync(_currentAccount, ct);
        await SwitchToNextAccountAsync(ct);
    }

    /// <summary>
    /// 创建带签名的请求。
    /// </summary>
    public async Task<IFlurlRequest> CreateSignedRequestAsync(
        XhsRequest request,
        HttpMethod method,
        string? bodyJson,
        CancellationToken ct,
        params object[] urlSegments)
    {
        IFlurlRequest flurlRequest = base.CreateFlurlRequest(request, method, urlSegments);

        // 调用签名服务获取 x-s, x-t 等
        var uri = flurlRequest.Url.Path;
        if (!string.IsNullOrEmpty(flurlRequest.Url.Query))
        {
            uri += "?" + flurlRequest.Url.Query;
        }

        var signResult = await _signClient.GetXhsSignAsync(new XhsSignRequest
        {
            Uri = uri,
            Data = bodyJson,
            Cookies = Cookies
        }, ct);

        return flurlRequest
            .WithHeader("X-s", signResult.XS)
            .WithHeader("X-t", signResult.XT)
            .WithHeader("x-s-common", signResult.XSCommon)
            .WithHeader("X-B3-Traceid", signResult.XB3TraceId)
            .WithHeader("Cookie", Cookies)
            .WithHeader("Accept", "application/json, text/plain, */*")
            .WithHeader("Accept-Language", "zh-CN,zh;q=0.9")
            .WithHeader("Content-Type", "application/json;charset=UTF-8")
            .WithHeader("Origin", ClientOptions.Origin)
            .WithHeader("Referer", ClientOptions.Referer)
            .WithHeader("User-Agent", ClientOptions.UserAgent);
    }

    /// <summary>
    /// 发送签名 GET 请求。
    /// </summary>
    public async Task<T> SendSignedGetAsync<T>(
        XhsRequest request,
        CancellationToken ct = default,
        params object[] urlSegments)
        where T : XhsResponse, new()
    {
        var flurlRequest = await CreateSignedRequestAsync(request, HttpMethod.Get, null, ct, urlSegments);
        using IFlurlResponse response = await base.SendFlurlRequestAsync(flurlRequest, null, ct);
        return await WrapFlurlResponseAsJsonAsync<T>(response, ct);
    }

    /// <summary>
    /// 发送签名 POST 请求。
    /// </summary>
    public async Task<T> SendSignedPostAsync<T>(
        XhsRequest request,
        object? data = null,
        CancellationToken ct = default,
        params object[] urlSegments)
        where T : XhsResponse, new()
    {
        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        string? bodyJson = data is not null ? System.Text.Json.JsonSerializer.Serialize(data, jsonOptions) : null;

        var flurlRequest = await CreateSignedRequestAsync(request, HttpMethod.Post, bodyJson, ct, urlSegments);
        using IFlurlResponse response = await base.SendFlurlRequestAsJsonAsync(flurlRequest, data, ct);
        return await WrapFlurlResponseAsJsonAsync<T>(response, ct);
    }
}
