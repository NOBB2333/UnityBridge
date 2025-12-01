using UnityBridge.Tools.Utils;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// URLHelper 使用示例
/// </summary>
public static class URLHelperExamples
{
    public static void Run()
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
}

