namespace UnityBridge.Crawler.Core.SignService;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 签名服务 HTTP 客户端，调用 MediaCrawlerPro-SignSrv。
/// 单一路由：/signsrv/v1/*
/// </summary>
public class SignServerClient : ISignClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly bool _disposeClient;

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = SignServerJsonSerializerContext.Default
    };

    /// <summary>
    /// 初始化签名服务客户端。
    /// </summary>
    /// <param name="signServerUrl">签名服务地址（如 http://localhost:8888）。</param>
    /// <param name="httpClient">可选的 HttpClient 实例。</param>
    public SignServerClient(string signServerUrl = "http://localhost:8888", HttpClient? httpClient = null)
    {
        _baseUrl = signServerUrl.TrimEnd('/');
        _disposeClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>把小红书请求打包后发到 signsrv，取回 x-s/x-t 等签名。</summary>
    public async Task<XhsSignResult> GetXhsSignAsync(XhsSignRequest request, CancellationToken ct = default)
    {
        var payload = new XhsSignPayload
        {
            Uri = request.Uri,
            Data = request.Data,
            Cookies = request.Cookies
        };

        var response = await PostJsonAsync<XhsSignPayload, SignServerResponse<XhsSignResult>>(
            signSrvPath: "/signsrv/v1/xhs/sign",
            payload: payload,
            ct: ct);
        return response?.Data ?? new XhsSignResult();
    }

    /// <summary>把抖音参数送到 signsrv，拿回 a-bogus 等签名字段。</summary>
    public async Task<DouyinSignResult> GetDouyinSignAsync(DouyinSignRequest request, CancellationToken ct = default)
    {
        var payload = new DouyinSignPayload
        {
            Uri = request.Uri,
            Cookies = request.Cookies
        };

        var response = await PostJsonAsync<DouyinSignPayload, SignServerResponse<DouyinSignResult>>(
            signSrvPath: "/signsrv/v1/douyin/sign",
            payload: payload,
            ct: ct);
        return response?.Data ?? new DouyinSignResult();
    }

    /// <summary>把快手签名请求投递给 signsrv，回填 did 等结果。</summary>
    public async Task<KuaishouSignResult> GetKuaishouSignAsync(KuaishouSignRequest request, CancellationToken ct = default)
    {
        var payload = new KuaishouSignPayload
        {
            Uri = request.Uri,
            Cookies = request.Cookies
        };

        var response = await PostJsonAsync<KuaishouSignPayload, SignServerResponse<KuaishouSignResult>>(
            signSrvPath: "/signsrv/v1/kuaishou/sign",
            payload: payload,
            ct: ct);
        return response?.Data ?? new KuaishouSignResult();
    }

    /// <summary>把 B 站参数送到 signsrv，生成 wts/w_rid 返回给调用方。</summary>
    public async Task<BilibiliSignResult> GetBilibiliSignAsync(BilibiliSignRequest request, CancellationToken ct = default)
    {
        var payload = new BilibiliSignPayload
        {
            ReqData = request.ReqData,
            Cookies = request.Cookies
        };

        var response = await PostJsonAsync<BilibiliSignPayload, SignServerResponse<BilibiliSignResult>>(
            signSrvPath: "/signsrv/v1/bilibili/sign",
            payload: payload,
            ct: ct);
        return response?.Data ?? new BilibiliSignResult();
    }

    /// <summary>把知乎请求交给 signsrv，拿回 x-zse-96/x-zst-81。</summary>
    public async Task<ZhihuSignResult> GetZhihuSignAsync(ZhihuSignRequest request, CancellationToken ct = default)
    {
        var payload = new ZhihuSignPayload
        {
            Uri = request.Uri,
            Cookies = request.Cookies
        };

        var response = await PostJsonAsync<ZhihuSignPayload, SignServerResponse<ZhihuSignResult>>(
            signSrvPath: "/signsrv/v1/zhihu/sign",
            payload: payload,
            ct: ct);
        return response?.Data ?? new ZhihuSignResult();
    }

    /// <summary>轻量探活一下 signsrv，确定服务在线再开始签名流程。</summary>
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{_baseUrl}/signsrv/pong", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<TResponse?> PostJsonAsync<TPayload, TResponse>(
        string signSrvPath,
        TPayload payload,
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, s_jsonSerializerOptions);
        using var content = new StringContent(json);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        using var response = await _httpClient.PostAsync($"{_baseUrl}{signSrvPath}", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TResponse>(responseJson, s_jsonSerializerOptions);
    }

    public void Dispose()
    {
        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }
}

/// <summary>签名服务通用响应包装。</summary>
internal class SignServerResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

internal sealed class XhsSignPayload
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("cookies")]
    public string Cookies { get; set; } = string.Empty;
}

internal sealed class DouyinSignPayload
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("cookies")]
    public string Cookies { get; set; } = string.Empty;
}

internal sealed class KuaishouSignPayload
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("cookies")]
    public string Cookies { get; set; } = string.Empty;
}

internal sealed class BilibiliSignPayload
{
    [JsonPropertyName("req_data")]
    public Dictionary<string, string> ReqData { get; set; } = new();

    [JsonPropertyName("cookies")]
    public string Cookies { get; set; } = string.Empty;
}

internal sealed class ZhihuSignPayload
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("cookies")]
    public string Cookies { get; set; } = string.Empty;
}

[JsonSerializable(typeof(XhsSignPayload))]
[JsonSerializable(typeof(DouyinSignPayload))]
[JsonSerializable(typeof(KuaishouSignPayload))]
[JsonSerializable(typeof(BilibiliSignPayload))]
[JsonSerializable(typeof(ZhihuSignPayload))]
[JsonSerializable(typeof(SignServerResponse<XhsSignResult>))]
[JsonSerializable(typeof(SignServerResponse<DouyinSignResult>))]
[JsonSerializable(typeof(SignServerResponse<KuaishouSignResult>))]
[JsonSerializable(typeof(SignServerResponse<BilibiliSignResult>))]
[JsonSerializable(typeof(SignServerResponse<ZhihuSignResult>))]
[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false)]
internal partial class SignServerJsonSerializerContext : JsonSerializerContext
{
}
