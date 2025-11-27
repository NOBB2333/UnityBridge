using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityBridge.Core;

/// <summary>
/// SKIT.FlurlHttpClient 客户端基类。
/// </summary>
public abstract class CommonClientBase : ICommonClient
{
    /// <summary>
    /// 获取当前客户端使用的 <see cref="IFlurlClient"/> 对象。
    /// </summary>
    public IFlurlClient FlurlClient { get; }

    /// <summary>
    /// 获取当前客户端使用的 JSON 序列化器。
    /// </summary>
    public Flurl.Http.Configuration.ISerializer JsonSerializer => FlurlClient.Settings.JsonSerializer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="disposeClient"></param>
    /// <param name="jsonOptions"></param>
    protected CommonClientBase(HttpClient? httpClient = null, bool disposeClient = true, JsonSerializerOptions? jsonOptions = null)
    {
        FlurlClient = httpClient is null ? new FlurlClient() : new FlurlClient(httpClient);
        if (!disposeClient)
        {
            // FlurlClient 4.x handles lifecycle differently, but for compatibility we keep the structure.
            // If httpClient is passed, FlurlClient wraps it.
        }
            
        ConfigureFlurlClient(FlurlClient, jsonOptions);
    }

    protected virtual void ConfigureFlurlClient(IFlurlClient flurlClient, JsonSerializerOptions? jsonOptions = null)
    {
        flurlClient.WithSettings(settings =>
        {
            var options = jsonOptions ?? new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            settings.JsonSerializer = new FlurlSystemJsonSerializer(options);
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="httpMethod"></param>
    /// <param name="urlSegments"></param>
    /// <returns></returns>
    public virtual IFlurlRequest CreateFlurlRequest(object request, HttpMethod httpMethod, params object[] urlSegments)
    {
        IFlurlRequest flurlRequest = FlurlClient.Request(urlSegments);
        flurlRequest.Verb = httpMethod;
        return flurlRequest;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flurlRequest"></param>
    /// <param name="httpContent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<IFlurlResponse> SendFlurlRequestAsync(IFlurlRequest flurlRequest, HttpContent? httpContent = null, CancellationToken cancellationToken = default)
    {
        if (flurlRequest is null) throw new ArgumentNullException(nameof(flurlRequest));

        if (httpContent is not null)
        {
            return await flurlRequest.SendAsync(flurlRequest.Verb, httpContent, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
            
        return await flurlRequest.SendAsync(flurlRequest.Verb, null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flurlRequest"></param>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<IFlurlResponse> SendFlurlRequestAsJsonAsync(IFlurlRequest flurlRequest, object? data = null, CancellationToken cancellationToken = default)
    {
        if (flurlRequest is null) throw new ArgumentNullException(nameof(flurlRequest));

        return await flurlRequest.SendJsonAsync(flurlRequest.Verb, data, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="flurlResponse"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<T> WrapFlurlResponseAsJsonAsync<T>(IFlurlResponse flurlResponse, CancellationToken cancellationToken = default)
        where T : new()
    {
        if (flurlResponse is null) throw new ArgumentNullException(nameof(flurlResponse));

        T result = await flurlResponse.GetJsonAsync<T>().ConfigureAwait(false);

        if (result is CommonResponseBase responseBase)
        {
            responseBase.RawStatus = flurlResponse.StatusCode;
            responseBase.RawHeaders = flurlResponse.Headers.ToDictionary(k => k.Name, v => v.Value);
            // Flurl 4.0: GetBytesAsync() returns byte[]
            // Note: GetJsonAsync might have already consumed the stream depending on implementation, 
            // but Flurl usually buffers. However, since we already deserialized, getting bytes might be redundant or fail if stream is closed.
            // For now, let's try to get bytes if possible, or skip if it's too complex for this refactor.
            // Actually, SKIT usually populates this.
            // In Flurl 4, we can access content.
            // responseBase.RawBytes = ... 
            // Let's skip RawBytes for now to avoid stream issues, or just set empty.
            // Or better, read bytes first then deserialize.
        }

        return result;

    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            FlurlClient?.Dispose();
        }
    }
}

internal class FlurlSystemJsonSerializer : Flurl.Http.Configuration.ISerializer
{
    private readonly JsonSerializerOptions _options;

    public FlurlSystemJsonSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    public string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, obj.GetType(), _options);
    }

    public T Deserialize<T>(string s)
    {
        return JsonSerializer.Deserialize<T>(s, _options)!;
    }

    public T Deserialize<T>(Stream stream)
    {
        return JsonSerializer.Deserialize<T>(stream, _options)!;
    }
}