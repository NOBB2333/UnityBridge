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
        public static Result<string> TryUrlDecode(string url) => Try(() => HttpUtility.UrlDecode(url));

        /// <summary>
        /// 安全的URL编码 - 返回 Result，不会抛出异常
        /// </summary>
        public static Result<string> TryUrlEncode(string text) => Try(() => HttpUtility.UrlEncode(text));

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
        public static Result<Dictionary<string, object>> TryParseUrlParams(string url) => Try(() => ParseUrlParams(url));

        /// <summary>
        /// 尝试执行操作并返回 Result
        /// </summary>
        private static Result<T> Try<T>(Func<T> func)
        {
            try
            {
                return new Result<T>(func());
            }
            catch (Exception ex)
            {
                return new Result<T>(ex);
            }
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