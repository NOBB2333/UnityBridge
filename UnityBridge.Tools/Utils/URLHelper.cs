using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using DotNext;

namespace UnityBridge.Tools.Utils
{
    /// <summary>
    /// URL处理工具类
    /// 提供URL解码、URL转字典、字典转URL等常用功能
    /// </summary>
    public static class URLHelper
    {
        /// <summary>
        /// URL解码 - 直接返回结果，异常会抛出
        /// </summary>
        public static string UrlDecode(string url) => HttpUtility.UrlDecode(url);

        /// <summary>
        /// URL编码 - 直接返回结果，异常会抛出
        /// </summary>
        public static string UrlEncode(string text) => HttpUtility.UrlEncode(text);

        /// <summary>
        /// 安全的URL解码 - 返回 Result，不会抛出异常
        /// </summary>
        public static Result<string> TryUrlDecode(string url)
        {
            if (url == null) return new Result<string>(new ArgumentNullException(nameof(url)));
            return new Result<string>(HttpUtility.UrlDecode(url));
        }

        /// <summary>
        /// 安全的URL编码 - 返回 Result，不会抛出异常
        /// </summary>
        public static Result<string> TryUrlEncode(string text)
        {
            if (text == null) return new Result<string>(new ArgumentNullException(nameof(text)));
            return new Result<string>(HttpUtility.UrlEncode(text));
        }

        /// <summary>
        /// 解析URL参数为字典
        /// </summary>
        public static Dictionary<string, object> ParseUrlParams(string url)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(url)) return result;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (url.Contains("?"))
                {
                    var queryPart = url.Substring(url.IndexOf('?') + 1);
                    var queryParams = HttpUtility.ParseQueryString(queryPart);
                    foreach (string? key in queryParams.AllKeys)
                    {
                        if (key == null) continue;
                        var values = queryParams.GetValues(key);
                        if (values != null)
                        {
                            result[key] = values.Length == 1 ? values[0] : new List<string>(values);
                        }
                    }
                }
                return result;
            }

            var query = HttpUtility.ParseQueryString(uri.Query);
            foreach (string? key in query.AllKeys)
            {
                if (key == null) continue;
                var values = query.GetValues(key);
                if (values != null)
                {
                    result[key] = values.Length == 1 ? values[0] : new List<string>(values);
                }
            }

            return result;
        }

        /// <summary>
        /// 安全的解析URL参数 - 返回 Result
        /// </summary>
        public static Result<Dictionary<string, object>> TryParseUrlParams(string url)
        {
            if (string.IsNullOrEmpty(url)) 
                return new Result<Dictionary<string, object>>(new Dictionary<string, object>());

            // 尝试创建 URI，如果失败则尝试作为相对路径处理（兼容 ParseUrlParams 的逻辑）
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                // 如果不是绝对 URI，检查是否包含查询字符串
                if (url.Contains("?"))
                {
                    // 这里我们复用 ParseUrlParams 的逻辑，因为它本身就是安全的（不抛出异常，只是返回空或部分结果）
                    // 但为了符合 "Try" 的语义，如果解析完全失败（比如格式严重错误），应该返回 Failure。
                    // 不过 ParseUrlParams 目前的设计是宽容的。
                    // 为了高性能，我们可以直接调用 ParseUrlParams，因为它内部没有 try-catch。
                    // 但用户希望 TryParseUrlParams 是 explicit safe via Result.
                    
                    // 让我们直接调用 ParseUrlParams，因为它现在是"安全"的（不抛异常）。
                    // 如果未来 ParseUrlParams 变了，这里可能需要调整。
                    // 但为了满足 Result 的形式：
                    return new Result<Dictionary<string, object>>(ParseUrlParams(url));
                }
                
                // 如果既不是 URI 也没有 ?，那可能只是一个路径，返回空字典
                return new Result<Dictionary<string, object>>(new Dictionary<string, object>());
            }

            // 如果是有效 URI，直接解析
            return new Result<Dictionary<string, object>>(ParseUrlParams(url));
        }

        /// <summary>
        /// 将字典转换为URL参数字符串
        /// </summary>
        public static string DictToUrlParams(Dictionary<string, object> paramsDict)
        {
            if (paramsDict == null || paramsDict.Count == 0) return "";

            var query = HttpUtility.ParseQueryString(string.Empty);
            foreach (var kvp in paramsDict)
            {
                if (kvp.Value is IEnumerable<string> list)
                {
                    foreach (var item in list) query.Add(kvp.Key, item);
                }
                else if (kvp.Value is System.Collections.IEnumerable enumerable && kvp.Value is not string)
                {
                    foreach (var item in enumerable) query.Add(kvp.Key, item?.ToString());
                }
                else
                {
                    query.Add(kvp.Key, kvp.Value?.ToString());
                }
            }

            return query.ToString() ?? "";
        }

        /// <summary>
        /// 构建完整的URL
        /// </summary>
        public static string BuildUrl(string baseUrl, Dictionary<string, object>? paramsDict = null,
            string? fragment = null)
        {
            var url = baseUrl;
            if (paramsDict != null && paramsDict.Count > 0)
            {
                var paramString = DictToUrlParams(paramsDict);
                var separator = baseUrl.Contains("?") ? "&" : "?";
                url = $"{baseUrl}{separator}{paramString}";
            }

            if (!string.IsNullOrEmpty(fragment))
            {
                url = $"{url}#{fragment}";
            }

            return url;
        }

        /// <summary>
        /// 获取URL的域名 - 返回 Optional
        /// </summary>
        public static Optional<string> GetDomain(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                ? Optional.Some(uri.Host)
                : Optional.None<string>();

        /// <summary>
        /// 获取URL的路径部分 - 返回 Optional
        /// </summary>
        public static Optional<string> GetPath(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                ? Optional.Some(uri.AbsolutePath)
                : Optional.None<string>();

        /// <summary>
        /// 验证URL是否有效
        /// </summary>
        public static bool IsValidUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        /// <summary>
        /// 拼接URL路径
        /// </summary>
        public static string JoinUrl(string baseUrl, params string[] paths)
        {
            var sb = new StringBuilder(baseUrl.TrimEnd('/'));
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                sb.Append('/');
                sb.Append(path.Trim('/'));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 移除URL中的参数
        /// </summary>
        public static string RemoveParams(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
            }

            var queryIndex = url.IndexOf('?');
            return queryIndex >= 0 ? url.Substring(0, queryIndex) : url;
        }

        /// <summary>
        /// 更新URL参数
        /// </summary>
        public static string UpdateUrlParams(string url, Dictionary<string, object> paramsDict)
        {
            var existingParams = ParseUrlParams(url);
            foreach (var kvp in paramsDict)
            {
                existingParams[kvp.Key] = kvp.Value;
            }

            var baseUrl = RemoveParams(url);
            return BuildUrl(baseUrl, existingParams);
        }
    }
}