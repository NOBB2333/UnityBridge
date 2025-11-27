using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityBridge.Tools;

/// <summary>
/// 网络请求工具类，封装 HttpClient，支持重试、代理、Cookie 管理等。
/// </summary>
public class NetworkHelper : IDisposable
{
    private HttpClient _client;
    private readonly HttpClientHandler _handler;
    private readonly int _maxRetries;
    private readonly TimeSpan _timeout;
    private readonly Dictionary<string, string> _cookies = new();
    private readonly Dictionary<string, string> _headers = new();
    private string? _proxy;
    private string _userAgent = "Mozilla/5.0 (C# NetworkHelper)";

    /// <summary>
    /// 初始化 NetworkHelper。
    /// </summary>
    /// <param name="timeoutSecs">超时时间（秒）</param>
    /// <param name="maxRetries">最大重试次数</param>
    public NetworkHelper(int timeoutSecs, int maxRetries)
    {
        _timeout = TimeSpan.FromSeconds(timeoutSecs);
        _maxRetries = Math.Max(1, maxRetries);
        _handler = new HttpClientHandler
        {
            UseCookies = false,
            AllowAutoRedirect = true
        };
        _client = new HttpClient(_handler) { Timeout = _timeout };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
    }

    /// <summary>
    /// 设置 HTTP 代理。
    /// </summary>
    /// <param name="proxy">代理地址 (e.g., "http://127.0.0.1:8080")，传 null 清除代理</param>
    public void SetProxy(string? proxy)
    {
        _proxy = proxy;
        if (!string.IsNullOrEmpty(proxy))
        {
            _handler.Proxy = new WebProxy(proxy);
            _handler.UseProxy = true;
        }
        else
        {
            _handler.UseProxy = false;
        }
        _client.Dispose();
        _client = new HttpClient(_handler) { Timeout = _timeout };
        ApplyHeaders();
    }

    /// <summary>
    /// 设置 User-Agent。
    /// </summary>
    public void SetUserAgent(string userAgent)
    {
        _userAgent = userAgent;
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    }

    /// <summary>
    /// 批量设置 Cookies。
    /// </summary>
    public void SetCookies(Dictionary<string, string> cookies)
    {
        _cookies.Clear();
        foreach (var kvp in cookies)
        {
            _cookies[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// 添加单个 Cookie。
    /// </summary>
    public void AddCookie(string key, string value)
    {
        _cookies[key] = value;
    }

    /// <summary>
    /// 获取当前所有 Cookies。
    /// </summary>
    public Dictionary<string, string> GetCookies()
    {
        return new Dictionary<string, string>(_cookies);
    }

    /// <summary>
    /// 清除所有 Cookies。
    /// </summary>
    public void ClearCookies()
    {
        _cookies.Clear();
    }

    /// <summary>
    /// 设置全局请求头。
    /// </summary>
    public void SetHeaders(Dictionary<string, string> headers)
    {
        foreach (var kvp in headers)
        {
            _headers[kvp.Key] = kvp.Value;
        }
        ApplyHeaders();
    }

    /// <summary>
    /// 获取当前全局请求头。
    /// </summary>
    public Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>(_headers);
    }

    private void ApplyHeaders()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        foreach (var kvp in _headers)
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (_cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ", _cookies.Select(c => $"{c.Key}={c.Value}"));
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }
        return request;
    }

    private async Task<HttpResponseMessage> RequestWithRetriesAsync(HttpMethod method, string url, Func<HttpRequestMessage> requestFactory)
    {
        Exception? lastEx = null;
        for (int i = 0; i < _maxRetries; i++)
        {
            try
            {
                var request = requestFactory();
                return await _client.SendAsync(request);
            }
            catch (Exception ex)
            {
                lastEx = ex;
            }
        }
        throw lastEx ?? new Exception($"Request failed after {_maxRetries} attempts");
    }

    private HttpResponseMessage RequestWithRetries(HttpMethod method, string url, Func<HttpRequestMessage> requestFactory)
    {
        return RequestWithRetriesAsync(method, url, requestFactory).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 发送 GET 请求。
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <param name="query">查询参数 (可选)</param>
    /// <param name="headers">临时请求头 (可选)</param>
    /// <returns>响应内容字符串</returns>
    public string Get(string url, Dictionary<string, string>? query = null, Dictionary<string, string>? headers = null)
    {
        if (query != null)
        {
            var uriBuilder = new UriBuilder(url);
            var queryParams = ParseQueryString(uriBuilder.Query);
            foreach (var kvp in query)
            {
                if (!queryParams.ContainsKey(kvp.Key))
                {
                    queryParams[kvp.Key] = new List<string>();
                }
                queryParams[kvp.Key].Add(kvp.Value);
            }
            uriBuilder.Query = BuildQueryString(queryParams);
            url = uriBuilder.ToString();
        }

        return RequestWithRetries(HttpMethod.Get, url, () =>
        {
            var req = CreateRequest(HttpMethod.Get, url);
            if (headers != null)
            {
                foreach (var h in headers) req.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
            return req;
        }).Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 发送 POST 请求。
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <param name="data">表单数据 (application/x-www-form-urlencoded)</param>
    /// <param name="json">JSON 对象 (application/json)，data 和 json 二选一</param>
    /// <returns>响应内容字符串</returns>
    public string Post(string url, Dictionary<string, string>? data = null, object? json = null)
    {
        return RequestWithBody(HttpMethod.Post, url, data, json);
    }

    /// <summary>
    /// 发送 PUT 请求。
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <param name="data">表单数据</param>
    /// <param name="json">JSON 对象</param>
    /// <returns>响应内容字符串</returns>
    public string Put(string url, Dictionary<string, string>? data = null, object? json = null)
    {
        return RequestWithBody(HttpMethod.Put, url, data, json);
    }

    /// <summary>
    /// 发送 DELETE 请求。
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <returns>响应内容字符串</returns>
    public string Delete(string url)
    {
        return RequestWithRetries(HttpMethod.Delete, url, () => CreateRequest(HttpMethod.Delete, url))
            .Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    private string RequestWithBody(HttpMethod method, string url, Dictionary<string, string>? data, object? json)
    {
        return RequestWithRetries(method, url, () =>
        {
            var req = CreateRequest(method, url);
            if (json != null)
            {
                var jsonStr = System.Text.Json.JsonSerializer.Serialize(json);
                req.Content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
            }
            else if (data != null)
            {
                req.Content = new FormUrlEncodedContent(data);
            }
            return req;
        }).Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 下载文件。
    /// </summary>
    /// <param name="url">文件 URL</param>
    /// <param name="filePath">保存路径</param>
    public void DownloadFile(string url, string filePath)
    {
        var response = RequestWithRetries(HttpMethod.Get, url, () => CreateRequest(HttpMethod.Get, url));
        using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var fileStream = new FileStream(filePath, FileMode.Create);
        stream.CopyTo(fileStream);
    }

    /// <summary>
    /// 检查 URL 状态。
    /// </summary>
    /// <returns>(是否成功, 状态码)</returns>
    public (bool, int) CheckUrlStatus(string url)
    {
        try
        {
            var response = RequestWithRetries(HttpMethod.Get, url, () => CreateRequest(HttpMethod.Get, url));
            return (response.IsSuccessStatusCode, (int)response.StatusCode);
        }
        catch
        {
            return (false, 0);
        }
    }

    /// <summary>
    /// 获取网页标题。
    /// </summary>
    public string GetPageTitle(string url)
    {
        try
        {
            var html = Get(url);
            var match = Regex.Match(html, @"(?is)<title>(.*?)</title>");
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取重定向后的最终 URL。
    /// </summary>
    public string GetRedirectUrl(string url)
    {
        var response = RequestWithRetries(HttpMethod.Get, url, () => CreateRequest(HttpMethod.Get, url));
        return response.RequestMessage?.RequestUri?.ToString() ?? url;
    }

    /// <summary>
    /// 判断 URL 是否可访问（状态码 2xx）。
    /// </summary>
    public bool IsUrlAccessible(string url)
    {
        return CheckUrlStatus(url).Item1;
    }

    /// <summary>
    /// 获取域名的 IP 地址。
    /// </summary>
    public string GetDomainIp(string domain)
    {
        try
        {
            var addresses = Dns.GetHostAddresses(domain);
            return addresses.FirstOrDefault()?.ToString() ?? throw new Exception("No IP found");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to resolve domain: {ex.Message}");
        }
    }

    /// <summary>
    /// Ping 主机。
    /// </summary>
    /// <param name="host">主机名或 IP</param>
    /// <param name="timeoutSecs">超时时间（秒）</param>
    /// <returns>是否 Ping 通 (TCP 80 端口连接测试)</returns>
    public bool PingHost(string host, int timeoutSecs)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, 80, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(timeoutSecs));
            if (!success) return false;
            client.EndConnect(result);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取响应的基本信息 (状态码, URL, 内容长度)。
    /// </summary>
    public Dictionary<string, string> GetResponseInfo(string url)
    {
        var response = RequestWithRetries(HttpMethod.Get, url, () => CreateRequest(HttpMethod.Get, url));
        return new Dictionary<string, string>
        {
            { "status_code", ((int)response.StatusCode).ToString() },
            { "url", response.RequestMessage?.RequestUri?.ToString() ?? url },
            { "content_length", response.Content.Headers.ContentLength?.ToString() ?? "0" },
            { "elapsed", "0" }
        };
    }

    /// <summary>
    /// 批量发送 GET 请求。
    /// </summary>
    /// <param name="urls">URL 列表</param>
    /// <returns>URL 到响应内容的字典</returns>
    public Dictionary<string, string> BatchRequest(List<string> urls)
    {
        var results = new Dictionary<string, string>();
        foreach (var url in urls)
        {
            try
            {
                results[url] = Get(url);
            }
            catch (Exception ex)
            {
                results[url] = $"Error: {ex.Message}";
            }
        }
        return results;
    }

    #region Static URL Helpers

    /// <summary>
    /// URL 解码。
    /// </summary>
    public static string UrlDecode(string url) => WebUtility.UrlDecode(url);
    
    /// <summary>
    /// URL 编码。
    /// </summary>
    public static string UrlEncode(string text) => WebUtility.UrlEncode(text);

    /// <summary>
    /// 解析 URL 参数为字典。
    /// </summary>
    public static Dictionary<string, List<string>> ParseUrlParams(string url)
    {
        var uri = new Uri(url);
        return ParseQueryString(uri.Query);
    }

    /// <summary>
    /// 将参数字典转换为 URL 查询字符串。
    /// </summary>
    public static string DictToUrlParams(Dictionary<string, List<string>> paramsDict)
    {
        return BuildQueryString(paramsDict);
    }

    /// <summary>
    /// 构建完整 URL。
    /// </summary>
    public static string BuildUrl(string baseUrl, Dictionary<string, List<string>>? paramsDict = null, string? fragment = null)
    {
        var uriBuilder = new UriBuilder(baseUrl);
        if (paramsDict != null)
        {
            uriBuilder.Query = BuildQueryString(paramsDict);
        }
        if (fragment != null)
        {
            uriBuilder.Fragment = fragment;
        }
        return uriBuilder.ToString();
    }

    /// <summary>
    /// 获取 URL 的域名。
    /// </summary>
    public static string GetDomain(string url) => new Uri(url).Host;
    
    /// <summary>
    /// 获取 URL 的路径部分。
    /// </summary>
    public static string GetPath(string url) => new Uri(url).AbsolutePath;
    
    /// <summary>
    /// 验证字符串是否为有效 URL。
    /// </summary>
    public static bool IsValidUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);

    /// <summary>
    /// 拼接 URL 片段。
    /// </summary>
    public static string JoinUrl(string baseUrl, params string[] segments)
    {
        var uri = new Uri(baseUrl);
        foreach (var segment in segments)
        {
            uri = new Uri(uri, segment);
        }
        return uri.ToString();
    }

    /// <summary>
    /// 移除 URL 中的查询参数。
    /// </summary>
    public static string RemoveParams(string url)
    {
        var uriBuilder = new UriBuilder(url) { Query = "" };
        return uriBuilder.ToString();
    }

    /// <summary>
    /// 更新 URL 中的查询参数。
    /// </summary>
    public static string UpdateUrlParams(string url, Dictionary<string, List<string>> paramsDict)
    {
        var uriBuilder = new UriBuilder(url);
        var queryParams = ParseQueryString(uriBuilder.Query);
        
        foreach (var kvp in paramsDict)
        {
            queryParams[kvp.Key] = kvp.Value;
        }
        
        uriBuilder.Query = BuildQueryString(queryParams);
        return uriBuilder.ToString();
    }

    #endregion

    #region Private Helpers

    private static Dictionary<string, List<string>> ParseQueryString(string query)
    {
        var result = new Dictionary<string, List<string>>();
        if (string.IsNullOrEmpty(query)) return result;

        if (query.StartsWith("?")) query = query.Substring(1);

        foreach (var part in query.Split('&'))
        {
            if (string.IsNullOrEmpty(part)) continue;
            var split = part.Split('=');
            var key = WebUtility.UrlDecode(split[0]);
            var value = split.Length > 1 ? WebUtility.UrlDecode(split[1]) : "";

            if (!result.ContainsKey(key))
            {
                result[key] = new List<string>();
            }
            result[key].Add(value);
        }
        return result;
    }

    private static string BuildQueryString(Dictionary<string, List<string>> queryParams)
    {
        var sb = new StringBuilder();
        foreach (var kvp in queryParams)
        {
            foreach (var val in kvp.Value)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(WebUtility.UrlEncode(kvp.Key));
                sb.Append('=');
                sb.Append(WebUtility.UrlEncode(val));
            }
        }
        return sb.ToString();
    }

    #endregion

    /// <summary>
    /// 释放资源。
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
        _handler.Dispose();
    }
}
