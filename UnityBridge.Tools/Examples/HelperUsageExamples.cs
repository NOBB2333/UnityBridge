using UnityBridge.Tools.Utils;
using DotNext;
using Newtonsoft.Json.Linq;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// URLHelper 和 JsonHelper 使用示例
/// </summary>
public static class HelperUsageExamples
{
    public static void RunAllExamples()
    {
        Console.WriteLine("=== URLHelper 使用示例 ===\n");
        URLHelperExamples();
        
        Console.WriteLine("\n=== JsonHelper 使用示例 ===\n");
        JsonHelperExamples();
        
        Console.WriteLine("\n=== 组合使用示例 ===\n");
        CombinedExample();
    }

    static void URLHelperExamples()
    {
        // 示例 1: 直接使用（会抛出异常）
        Console.WriteLine("1. 直接 URL 编码:");
        var encoded = URLHelper.UrlEncode("你好 世界");
        Console.WriteLine($"   编码结果: {encoded}");

        // 示例 2: 使用 Try* 方法（返回 Result，不抛异常）
        Console.WriteLine("\n2. 安全 URL 编码 (Result):");
        var encodeResult = URLHelper.TryUrlEncode("你好 世界");
        if (encodeResult.IsSuccessful)
        {
            Console.WriteLine($"   成功: {encodeResult.Value}");
        }
        else
        {
            Console.WriteLine($"   失败: {encodeResult.Error.Message}");
        }

        // 示例 3: 解析 URL 参数
        Console.WriteLine("\n3. 解析 URL 参数:");
        var url = "https://api.example.com/search?q=搜索&page=1&tags=tech&tags=news";
        var params1 = URLHelper.ParseUrlParams(url);
        Console.WriteLine($"   找到 {params1.Count} 个参数:");
        foreach (var kvp in params1)
        {
            Console.WriteLine($"   - {kvp.Key} = {kvp.Value}");
        }

        // 示例 4: 使用 Optional 获取域名
        Console.WriteLine("\n4. 获取域名 (Optional):");
        var domain = URLHelper.GetDomain("https://example.com/path?query=1");
        if (domain.HasValue)
        {
            Console.WriteLine($"   域名: {domain.Value}");
        }
        else
        {
            Console.WriteLine("   无效的 URL");
        }

        // 示例 5: 构建 URL
        Console.WriteLine("\n5. 构建 URL:");
        var newUrl = URLHelper.BuildUrl(
            "https://api.example.com/search",
            new Dictionary<string, object>
            {
                ["q"] = "关键词",
                ["page"] = 1,
                ["limit"] = 10
            },
            fragment: "results"
        );
        Console.WriteLine($"   构建的 URL: {newUrl}");

        // 示例 6: 更新 URL 参数
        Console.WriteLine("\n6. 更新 URL 参数:");
        var originalUrl = "https://example.com?old=value&keep=this";
        var updatedUrl = URLHelper.UpdateUrlParams(originalUrl, new Dictionary<string, object>
        {
            ["old"] = "newValue",
            ["new"] = "added"
        });
        Console.WriteLine($"   原始: {originalUrl}");
        Console.WriteLine($"   更新: {updatedUrl}");
    }

    static void JsonHelperExamples()
    {
        // 示例 1: 直接解析 JSON
        Console.WriteLine("1. 直接解析 JSON:");
        var jsonString = "{\"name\":\"张三\",\"age\":25,\"city\":\"北京\"}";
        var user = JsonHelper.ParseJson<Dictionary<string, object>>(jsonString);
        Console.WriteLine($"   姓名: {user?["name"]}");

        // 示例 2: 使用 Try* 方法安全解析
        Console.WriteLine("\n2. 安全解析 JSON (Result):");
        var parseResult = JsonHelper.TryParseJson<Dictionary<string, object>>(jsonString);
        if (parseResult.IsSuccessful)
        {
            Console.WriteLine($"   成功解析，姓名: {parseResult.Value["name"]}");
        }
        else
        {
            Console.WriteLine($"   解析失败: {parseResult.Error.Message}");
        }

        // 示例 3: 格式化 JSON
        Console.WriteLine("\n3. 格式化 JSON:");
        var minifiedJson = "{\"a\":1,\"b\":2}";
        var formatted = JsonHelper.FormatJson(minifiedJson);
        Console.WriteLine($"   格式化后:\n{formatted}");

        // 示例 4: 使用 Optional 获取 JSON 值
        Console.WriteLine("\n4. 获取 JSON 值 (Optional):");
        var token = JsonHelper.ParseJsonToken(jsonString);
        var name = JsonHelper.GetJsonValue<string>(token, "name");
        if (name.HasValue)
        {
            Console.WriteLine($"   姓名: {name.Value}");
        }

        var notExist = JsonHelper.GetJsonValue<string>(token, "notexist");
        Console.WriteLine($"   不存在的键: {(notExist.HasValue ? notExist.Value : "无值")}");

        // 示例 5: 扁平化 JSON
        Console.WriteLine("\n5. 扁平化 JSON:");
        var nestedJson = "{\"user\":{\"profile\":{\"name\":\"张三\",\"age\":25}}}";
        var nestedToken = JsonHelper.ParseJsonToken(nestedJson) as JObject;
        if (nestedToken != null)
        {
            var flattened = JsonHelper.FlattenJson(nestedToken);
            Console.WriteLine("   扁平化结果:");
            foreach (var kvp in flattened)
            {
                Console.WriteLine($"   - {kvp.Key} = {kvp.Value}");
            }
        }

        // 示例 6: 反扁平化
        Console.WriteLine("\n6. 反扁平化:");
        var flatDict = new Dictionary<string, object>
        {
            ["user.name"] = "李四",
            ["user.age"] = 30,
            ["user.address.city"] = "上海"
        };
        var unflattened = JsonHelper.UnflattenJson(flatDict);
        Console.WriteLine($"   反扁平化结果:\n{unflattened.ToString(Newtonsoft.Json.Formatting.Indented)}");

        // 示例 7: 合并 JSON
        Console.WriteLine("\n7. 合并 JSON:");
        var json1 = JObject.Parse("{\"a\":1,\"b\":{\"c\":2}}");
        var json2 = JObject.Parse("{\"b\":{\"d\":3},\"e\":4}");
        var merged = JsonHelper.MergeJson(json1, json2);
        Console.WriteLine($"   合并结果:\n{merged.ToString(Newtonsoft.Json.Formatting.Indented)}");
    }

    static void CombinedExample()
    {
        Console.WriteLine("组合使用 URLHelper 和 JsonHelper:");
        
        // 场景: 从 API URL 中提取 JSON 参数并格式化
        var apiUrl = "https://api.example.com/data?json=%7B%22name%22%3A%22%E5%BC%A0%E4%B8%89%22%7D";
        
        Console.WriteLine($"1. 原始 URL: {apiUrl}");
        
        // 解析 URL 参数
        var urlParams = URLHelper.ParseUrlParams(apiUrl);
        if (urlParams.TryGetValue("json", out var jsonParam))
        {
            Console.WriteLine($"2. 提取的 JSON 参数: {jsonParam}");
            
            // 解码 JSON 字符串
            var decodedJson = URLHelper.UrlDecode(jsonParam.ToString()!);
            Console.WriteLine($"3. 解码后: {decodedJson}");
            
            // 格式化 JSON
            var formatResult = JsonHelper.TryFormatJson(decodedJson);
            if (formatResult.IsSuccessful)
            {
                Console.WriteLine($"4. 格式化后:\n{formatResult.Value}");
            }
        }

        // 场景 2: 构建带 JSON 数据的 URL
        Console.WriteLine("\n场景 2: 构建带 JSON 数据的 URL");
        var data = new { name = "李四", age = 30 };
        var jsonData = JsonHelper.ToJsonString(data, indent: false);
        var encodedData = URLHelper.UrlEncode(jsonData);
        
        var finalUrl = URLHelper.BuildUrl(
            "https://api.example.com/submit",
            new Dictionary<string, object>
            {
                ["data"] = encodedData,
                ["format"] = "json"
            }
        );
        Console.WriteLine($"构建的 URL: {finalUrl}");
    }

    // 错误处理示例
    static void ErrorHandlingExample()
    {
        Console.WriteLine("=== 错误处理示例 ===\n");

        // 方式 1: 使用 try-catch（直接方法）
        Console.WriteLine("1. 使用 try-catch:");
        try
        {
            var result = JsonHelper.FormatJson("invalid json");
            Console.WriteLine($"   结果: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   捕获异常: {ex.Message}");
        }

        // 方式 2: 使用 Result（Try* 方法）
        Console.WriteLine("\n2. 使用 Result:");
        var formatResult = JsonHelper.TryFormatJson("invalid json");
        if (formatResult.IsSuccessful)
        {
            Console.WriteLine($"   成功: {formatResult.Value}");
        }
        else
        {
            Console.WriteLine($"   失败: {formatResult.Error.Message}");
        }

        // 方式 3: 使用 Optional
        Console.WriteLine("\n3. 使用 Optional:");
        var domain = URLHelper.GetDomain("not a valid url");
        if (domain.HasValue)
        {
            Console.WriteLine($"   域名: {domain.Value}");
        }
        else
        {
            Console.WriteLine("   无法获取域名（URL 无效）");
        }
    }
}
