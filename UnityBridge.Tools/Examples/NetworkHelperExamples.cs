using UnityBridge.Tools;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// NetworkHelper 使用示例
/// </summary>
public static class NetworkHelperExamples
{
    public static void Run()
    {
        // 示例 1: 创建 NetworkHelper
        Console.WriteLine("1. 创建 NetworkHelper:");
        using var helper = new NetworkHelper(timeoutSecs: 10, maxRetries: 3);
        Console.WriteLine("   已创建 NetworkHelper (超时: 10秒, 重试: 3次)");

        // 示例 2: 设置 User-Agent
        Console.WriteLine("\n2. 设置 User-Agent:");
        helper.SetUserAgent("MyApp/1.0");
        Console.WriteLine("   已设置 User-Agent");

        // 示例 3: 设置 Cookies
        Console.WriteLine("\n3. 设置 Cookies:");
        helper.SetCookies(new Dictionary<string, string>
        {
            ["session"] = "abc123",
            ["token"] = "xyz789"
        });
        var cookies = helper.GetCookies();
        Console.WriteLine($"   当前 Cookies: {cookies.Count} 个");
        foreach (var kvp in cookies)
        {
            Console.WriteLine($"   - {kvp.Key} = {kvp.Value}");
        }

        // 示例 4: 设置 Headers
        Console.WriteLine("\n4. 设置 Headers:");
        helper.SetHeaders(new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "CustomValue",
            ["Accept"] = "application/json"
        });
        Console.WriteLine("   已设置自定义 Headers");

        // 示例 5: GET 请求（示例，实际需要有效 URL）
        Console.WriteLine("\n5. GET 请求示例:");
        Console.WriteLine("   (注意: 需要有效的 URL 才能执行)");
        // var response = await helper.GetAsync("https://httpbin.org/get");
        // Console.WriteLine($"   响应状态: {response.StatusCode}");

        // 示例 6: POST 请求（示例）
        Console.WriteLine("\n6. POST 请求示例:");
        Console.WriteLine("   (注意: 需要有效的 URL 才能执行)");
        // var postData = new Dictionary<string, string> { ["key"] = "value" };
        // var postResponse = await helper.PostFormAsync("https://httpbin.org/post", postData);
        // Console.WriteLine($"   响应状态: {postResponse.StatusCode}");

        // 示例 7: 代理设置
        Console.WriteLine("\n7. 代理设置:");
        helper.SetProxy("http://127.0.0.1:8080");
        Console.WriteLine("   已设置代理: http://127.0.0.1:8080");
        helper.SetProxy(null);
        Console.WriteLine("   已清除代理");
    }
}

