using UnityBridge.Tools.Utils;
using Newtonsoft.Json.Linq;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// JsonHelper 使用示例
/// </summary>
public static class JsonHelperExamples
{
    public static void Run()
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
}

