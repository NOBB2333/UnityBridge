using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityBridge.Core.Clients;

/// <summary>
/// SKIT.FlurlHttpClient 客户端基类。
/// </summary>
public abstract class CommonClientBase
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
    /// 获取当前客户端使用的拦截器集合。
    /// </summary>
    public IList<HttpInterceptor> Interceptors { get; } = new List<HttpInterceptor>();

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

        // 创建拦截器上下文
        var context = new HttpInterceptorContext
        {
            FlurlRequest = flurlRequest
        };

        // 尝试捕获请求体（用于日志）
        if (httpContent is not null)
        {
            try
            {
                context.RequestBody = await httpContent.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // 忽略无法读取的情况
            }
        }

        // 执行请求前拦截器
        context.Stopwatch.Start();
        foreach (var interceptor in Interceptors)
        {
            await interceptor.BeforeCallAsync(context, cancellationToken).ConfigureAwait(false);
        }

        IFlurlResponse response;
        try
        {
            if (httpContent is not null)
            {
                response = await flurlRequest.SendAsync(flurlRequest.Verb, httpContent, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                response = await flurlRequest.SendAsync(flurlRequest.Verb, null, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            context.FlurlResponse = response;
        }
        catch (Exception ex)
        {
            context.Exception = ex;
            context.Stopwatch.Stop();

            // 执行异常后拦截器
            foreach (var interceptor in Interceptors)
            {
                await interceptor.AfterCallAsync(context, cancellationToken).ConfigureAwait(false);
            }
            throw;
        }

        context.Stopwatch.Stop();

        // 执行请求后拦截器
        foreach (var interceptor in Interceptors)
        {
            await interceptor.AfterCallAsync(context, cancellationToken).ConfigureAwait(false);
        }

        return response;
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

        // 创建拦截器上下文
        var context = new HttpInterceptorContext
        {
            FlurlRequest = flurlRequest,
            RequestBody = data is not null ? JsonSerializer.Serialize(data) : null
        };

        // 执行请求前拦截器
        context.Stopwatch.Start();
        foreach (var interceptor in Interceptors)
        {
            await interceptor.BeforeCallAsync(context, cancellationToken).ConfigureAwait(false);
        }

        IFlurlResponse response;
        try
        {
            response = await flurlRequest.SendJsonAsync(flurlRequest.Verb, data, cancellationToken: cancellationToken).ConfigureAwait(false);
            context.FlurlResponse = response;
        }
        catch (Exception ex)
        {
            context.Exception = ex;
            context.Stopwatch.Stop();

            // 执行异常后拦截器
            foreach (var interceptor in Interceptors)
            {
                await interceptor.AfterCallAsync(context, cancellationToken).ConfigureAwait(false);
            }
            throw;
        }

        context.Stopwatch.Stop();

        // 执行请求后拦截器
        foreach (var interceptor in Interceptors)
        {
            await interceptor.AfterCallAsync(context, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    /// <summary>
    /// 将 Flurl HTTP 响应包装为强类型响应对象。
    /// 先读取原始字节（保留 RawBytes），再反序列化 JSON，同时填充 RawStatus 和 RawHeaders。
    /// 注意：必须先读字节再反序列化，否则 Flurl 流消费后无法回读。
    /// </summary>
    /// <typeparam name="T">响应类型，需继承 <see cref="CommonResponseBase"/> 以获得 Raw* 字段填充。</typeparam>
    /// <param name="flurlResponse">Flurl 响应对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>反序列化后的响应对象。</returns>
    public virtual async Task<T> WrapFlurlResponseAsJsonAsync<T>(IFlurlResponse flurlResponse, CancellationToken cancellationToken = default)
        where T : new()
    {
        if (flurlResponse is null) throw new ArgumentNullException(nameof(flurlResponse));

        var rawBytes = await flurlResponse.GetBytesAsync().ConfigureAwait(false);
        var jsonString = System.Text.Encoding.UTF8.GetString(rawBytes);
        T result = FlurlClient.Settings.JsonSerializer.Deserialize<T>(jsonString) ?? new T();

        if (result is CommonResponseBase responseBase)
        {
            responseBase.RawStatus = flurlResponse.StatusCode;
            responseBase.RawHeaders = flurlResponse.Headers
                .GroupBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g.Select(x => x.Value).Where(v => !string.IsNullOrWhiteSpace(v))),
                    StringComparer.OrdinalIgnoreCase);
            responseBase.RawBytes = rawBytes;
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
