using UnityBridge.Api.Dify.Settings;
using UnityBridge.Core;

namespace UnityBridge.Api.Dify;

/// <summary>
/// 一个 Dify API HTTP 客户端。
/// </summary>
public class DifyApiClient : CommonClientBase, ICommonClient
{
    /// <summary>
    /// 获取当前客户端使用的凭证。
    /// </summary>
    public Credentials Credentials { get; }

    /// <summary>
    /// 获取当前客户端使用的配置项。
    /// </summary>
    public DifyApiClientOptions ClientOptions { get; }

    /// <summary>
    /// 用指定的配置项初始化 <see cref="DifyApiClient"/> 类的新实例。
    /// </summary>
    /// <param name="options">配置项。</param>
    public DifyApiClient(DifyApiClientOptions options)
        : this(options, null)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="httpClient"></param>
    /// <param name="disposeClient"></param>
    /// <param name="jsonOptions"></param>
    public DifyApiClient(DifyApiClientOptions options, HttpClient? httpClient, bool disposeClient = true, System.Text.Json.JsonSerializerOptions? jsonOptions = null)
        : base(httpClient, disposeClient, jsonOptions)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        ClientOptions = options;
        Credentials = new Credentials(options);

        FlurlClient.BaseUrl = options.Endpoint ?? DifyApiEndpoints.DEFAULT;
        FlurlClient.WithTimeout(options.Timeout <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(options.Timeout));
    }

    /// <summary>
    /// 使用当前客户端生成一个新的 <see cref="IFlurlRequest"/> 对象。
    /// </summary>
    /// <param name="request"></param>
    /// <param name="httpMethod"></param>
    /// <param name="urlSegments"></param>
    /// <returns></returns>
    public IFlurlRequest CreateFlurlRequest(DifyApiRequest request, HttpMethod httpMethod, params object[] urlSegments)
    {
        IFlurlRequest flurlRequest = base.CreateFlurlRequest(request, httpMethod, urlSegments);

        if (request.AccessToken is not null)
        {
            flurlRequest.WithHeader("Authorization", $"Bearer {request.AccessToken}");
        }
        else if (Credentials.AppKey is not null)
        {
            flurlRequest.WithHeader("Authorization", $"Bearer {Credentials.AppKey}");
        }

        return flurlRequest;
    }

    /// <summary>
    /// 异步发起请求。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="flurlRequest"></param>
    /// <param name="httpContent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<T> SendFlurlRequestAsync<T>(IFlurlRequest flurlRequest, HttpContent? httpContent = null, CancellationToken cancellationToken = default)
        where T : DifyApiResponse, new()
    {
        if (flurlRequest is null) throw new ArgumentNullException(nameof(flurlRequest));

        using IFlurlResponse flurlResponse = await base.SendFlurlRequestAsync(flurlRequest, httpContent, cancellationToken).ConfigureAwait(false);
        return await WrapFlurlResponseAsJsonAsync<T>(flurlResponse, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步发起请求。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="flurlRequest"></param>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<T> SendFlurlRequestAsJsonAsync<T>(IFlurlRequest flurlRequest, object? data = null, CancellationToken cancellationToken = default)
        where T : DifyApiResponse, new()
    {
        if (flurlRequest is null) throw new ArgumentNullException(nameof(flurlRequest));

        bool isSimpleRequest = data is null ||
                               flurlRequest.Verb == HttpMethod.Get ||
                               flurlRequest.Verb == HttpMethod.Head ||
                               flurlRequest.Verb == HttpMethod.Options;
        using IFlurlResponse flurlResponse = isSimpleRequest ?
            await base.SendFlurlRequestAsync(flurlRequest, null, cancellationToken).ConfigureAwait(false) :
            await base.SendFlurlRequestAsJsonAsync(flurlRequest, data, cancellationToken).ConfigureAwait(false);
        return await WrapFlurlResponseAsJsonAsync<T>(flurlResponse, cancellationToken).ConfigureAwait(false);
    }
}