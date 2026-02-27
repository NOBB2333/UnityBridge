using UnityBridge.Crawler.Core;

namespace UnityBridge.Crawler.Kuaishou;

/// <summary>
/// 快手 API 客户端（GraphQL）。
/// </summary>
public class KuaishouClient : CrawlerClientBase
{
    /// <summary>
    /// 获取当前客户端使用的配置项。
    /// </summary>
    public new KuaishouClientOptions ClientOptions { get; }

    protected override string PlatformName => "kuaishou";

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
    /// 初始化快手客户端。
    /// </summary>
    public KuaishouClient(KuaishouClientOptions options, AccountPoolManager? accountPool = null)
        : base(options, jsonOptions: CreateAotJsonOptions())
    {
        ClientOptions = options ?? throw new ArgumentNullException(nameof(options));
        _accountPool = accountPool;

        FlurlClient.BaseUrl = options.Endpoint ?? KuaishouEndpoints.API;
        FlurlClient.WithTimeout(options.Timeout <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(options.Timeout));
    }

    private static JsonSerializerOptions CreateAotJsonOptions()
    {
        return new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = KuaishouJsonSerializerContext.Default
        };
    }

    /// <summary>
    /// 切换到下一个可用账号。
    /// </summary>
    public override async Task<bool> SwitchToNextAccountAsync(CancellationToken ct = default)
    {
        if (_accountPool is null) return false;

        _currentAccount = await _accountPool.GetAccountWithProxyAsync("kuaishou", ct);
        return _currentAccount is not null;
    }

    /// <summary>
    /// 发送 GraphQL POST 请求。
    /// </summary>
    public async Task<T> SendGraphQLAsync<T>(
        string operationName,
        object variables,
        string query,
        CancellationToken ct = default)
        where T : KuaishouResponse, new()
    {
        var payload = new
        {
            operationName,
            variables,
            query
        };

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        IFlurlRequest flurlRequest = FlurlClient.Request()
            .WithHeader("Accept", "application/json, text/plain, */*")
            .WithHeader("Content-Type", "application/json;charset=UTF-8")
            .WithHeader("Cookie", Cookies)
            .WithHeader("Origin", ClientOptions.Origin)
            .WithHeader("Referer", ClientOptions.Referer)
            .WithHeader("User-Agent", ClientOptions.UserAgent);

        flurlRequest.Verb = HttpMethod.Post;

        using IFlurlResponse response = await base.SendFlurlRequestAsync(
            flurlRequest,
            new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            ct);

        return await WrapFlurlResponseAsJsonAsync<T>(response, ct);
    }
}
