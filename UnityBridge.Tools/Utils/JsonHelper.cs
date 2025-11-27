using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DotNext;

namespace UnityBridge.Tools.Utils
{
    /// <summary>
    /// JSON处理工具类
    /// 提供JSON数据的解析、验证、转换、格式化等常用功能
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// 验证字符串是否为有效的JSON格式
        /// </summary>
        public static bool IsValidJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) return false;
            try
            {
                JToken.Parse(jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析JSON字符串 - 失败会抛出异常
        /// </summary>
        public static T? ParseJson<T>(string jsonString) => 
            JsonConvert.DeserializeObject<T>(jsonString);

        /// <summary>
        /// 安全解析JSON字符串 - 返回 Result
        /// </summary>
        public static Result<T> TryParseJson<T>(string jsonString)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(jsonString);
                return result != null ? new Result<T>(result) : new Result<T>(new InvalidOperationException("Deserialization returned null"));
            }
            catch (Exception ex)
            {
                return new Result<T>(ex);
            }
        }

        /// <summary>
        /// 解析JSON字符串为JToken - 失败会抛出异常
        /// </summary>
        public static JToken ParseJsonToken(string jsonString) => JToken.Parse(jsonString);

        /// <summary>
        /// 安全解析JSON为JToken - 返回 Result
        /// </summary>
        public static Result<JToken> TryParseJsonToken(string jsonString)
        {
            try
            {
                return new Result<JToken>(JToken.Parse(jsonString));
            }
            catch (Exception ex)
            {
                return new Result<JToken>(ex);
            }
        }

        /// <summary>
        /// 将对象转换为JSON字符串
        /// </summary>
        public static string ToJsonString(object data, bool indent = true)
        {
            var formatting = indent ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(data, formatting);
        }

        /// <summary>
        /// 格式化JSON字符串
        /// </summary>
        public static string FormatJson(string jsonString)
        {
            var obj = JToken.Parse(jsonString);
            return obj.ToString(Formatting.Indented);
        }

        /// <summary>
        /// 安全格式化JSON - 返回 Result
        /// </summary>
        public static Result<string> TryFormatJson(string jsonString)
        {
            try
            {
                return new Result<string>(FormatJson(jsonString));
            }
            catch (Exception ex)
            {
                return new Result<string>(ex);
            }
        }

        /// <summary>
        /// 压缩JSON字符串（移除空格和换行）
        /// </summary>
        public static string MinifyJson(string jsonString)
        {
            var obj = JToken.Parse(jsonString);
            return obj.ToString(Formatting.None);
        }

        /// <summary>
        /// 根据路径获取JSON值 - 返回 Optional
        /// </summary>
        public static Optional<T> GetJsonValue<T>(JToken data, string keyPath)
        {
            var token = data.SelectToken(keyPath);
            if (token == null) return Optional.None<T>();
            
            var value = token.ToObject<T>();
            return value != null ? Optional.Some(value) : Optional.None<T>();
        }

        /// <summary>
        /// 设置JSON值 - 返回是否成功
        /// </summary>
        public static bool SetJsonValue(JObject data, string keyPath, object value)
        {
            var token = data.SelectToken(keyPath);
            if (token != null)
            {
                token.Replace(JToken.FromObject(value));
                return true;
            }
            
            // Simple path (no dots or brackets)
            if (!keyPath.Contains(".") && !keyPath.Contains("["))
            {
                data[keyPath] = JToken.FromObject(value);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 合并JSON对象
        /// </summary>
        public static JObject MergeJson(JObject target, JObject source)
        {
            target.Merge(source, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            });
            return target;
        }

        /// <summary>
        /// 扁平化JSON对象
        /// </summary>
        public static Dictionary<string, object> FlattenJson(JObject data, string separator = ".")
        {
            var dict = new Dictionary<string, object>();
            Flatten(data, dict, "", separator);
            return dict;
        }

        private static void Flatten(JToken token, Dictionary<string, object> dict, string prefix, string separator)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (var prop in token.Children<JProperty>())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}{separator}{prop.Name}";
                    Flatten(prop.Value, dict, key, separator);
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                int index = 0;
                foreach (var item in token.Children())
                {
                    var key = string.IsNullOrEmpty(prefix) ? index.ToString() : $"{prefix}{separator}{index}";
                    Flatten(item, dict, key, separator);
                    index++;
                }
            }
            else
            {
                dict[prefix] = ((JValue)token).Value ?? "";
            }
        }

        /// <summary>
        /// 反扁平化JSON对象
        /// </summary>
        public static JObject UnflattenJson(Dictionary<string, object> data, string separator = ".")
        {
            var result = new JObject();
            foreach (var kvp in data)
            {
                var parts = kvp.Key.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                JToken current = result;
                
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    bool isLast = i == parts.Length - 1;
                    
                    if (current is JObject obj)
                    {
                        if (isLast)
                        {
                            obj[part] = JToken.FromObject(kvp.Value ?? "");
                        }
                        else
                        {
                            if (!obj.ContainsKey(part))
                            {
                                obj[part] = int.TryParse(parts[i + 1], out _) ? new JArray() : new JObject();
                            }
                            current = obj[part]!;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 提取JSON中指定键的所有值
        /// </summary>
        public static List<JToken> ExtractJsonValues(JToken data, string key)
        {
            var values = new List<JToken>();
            ExtractValuesRecursive(data, key, values);
            return values;
        }

        private static void ExtractValuesRecursive(JToken data, string key, List<JToken> values)
        {
            if (data.Type == JTokenType.Object)
            {
                var obj = (JObject)data;
                if (obj.ContainsKey(key))
                {
                    var value = obj[key];
                    if (value != null) values.Add(value);
                }
                foreach (var prop in obj.Properties())
                {
                    ExtractValuesRecursive(prop.Value, key, values);
                }
            }
            else if (data.Type == JTokenType.Array)
            {
                foreach (var item in data.Children())
                {
                    ExtractValuesRecursive(item, key, values);
                }
            }
        }

        /// <summary>
        /// 将JSON转换为XML格式
        /// </summary>
        public static string JsonToXml(string jsonString, string rootName = "root")
        {
            var doc = JsonConvert.DeserializeXmlNode(jsonString, rootName);
            return doc?.OuterXml ?? "";
        }

        /// <summary>
        /// 安全转换JSON到XML - 返回 Result
        /// </summary>
        public static Result<string> TryJsonToXml(string jsonString, string rootName = "root")
        {
            try
            {
                return new Result<string>(JsonToXml(jsonString, rootName));
            }
            catch (Exception ex)
            {
                return new Result<string>(ex);
            }
        }
    }
}
