using UnityBridge.Tools;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// StringHelper 使用示例
/// </summary>
public static class StringHelperExamples
{
    public static void Run()
    {
        // 示例 1: 字符串清洗
        Console.WriteLine("1. 字符串清洗:");
        var text = "  Hello    World  ";
        Console.WriteLine($"   原始: '{text}'");
        Console.WriteLine($"   去除空格: '{StringHelper.RemoveSpaces(text)}'");
        Console.WriteLine($"   合并空格: '{StringHelper.RemoveExtraSpaces(text)}'");

        // 示例 2: 字符串转换
        Console.WriteLine("\n2. 字符串转换:");
        var snakeCase = "hello_world_test";
        Console.WriteLine($"   下划线转驼峰: {StringHelper.ToCamelCase(snakeCase, '_')}");
        var camelCase = "helloWorldTest";
        Console.WriteLine($"   驼峰转下划线: {StringHelper.ToSnakeCase(camelCase)}");

        // 示例 3: 字符串反转和首字母大写
        Console.WriteLine("\n3. 字符串操作:");
        var original = "hello world";
        Console.WriteLine($"   反转: {StringHelper.ReverseString(original)}");
        Console.WriteLine($"   首字母大写: {StringHelper.CapitalizeWords(original)}");

        // 示例 4: 提取信息
        Console.WriteLine("\n4. 提取信息:");
        var mixedText = "联系我: 13812345678 或 email@example.com";
        var numbers = StringHelper.ExtractNumbers(mixedText);
        Console.WriteLine($"   提取数字: {string.Join(", ", numbers)}");
        var emails = StringHelper.ExtractEmails(mixedText);
        Console.WriteLine($"   提取邮箱: {string.Join(", ", emails)}");
        var phones = StringHelper.ExtractPhones(mixedText);
        Console.WriteLine($"   提取手机: {string.Join(", ", phones)}");

        // 示例 5: 验证
        Console.WriteLine("\n5. 验证:");
        Console.WriteLine($"   邮箱验证: {StringHelper.IsEmail("test@example.com")}");
        Console.WriteLine($"   手机验证: {StringHelper.IsPhone("13812345678")}");
        Console.WriteLine($"   身份证验证: {StringHelper.IsIdCard("110101199001011234")}");

        // 示例 6: 脱敏处理
        Console.WriteLine("\n6. 脱敏处理:");
        var sensitive = "13812345678";
        var masked = StringHelper.MaskSensitiveInfo(sensitive, '*', 3, 4);
        Console.WriteLine($"   脱敏: {masked}");

        // 示例 7: 统计
        Console.WriteLine("\n7. 统计:");
        var chineseText = "Hello 世界 World";
        Console.WriteLine($"   单词数: {StringHelper.CountWords(chineseText)}");
        Console.WriteLine($"   中文字符数: {StringHelper.CountChineseChars(chineseText)}");

        // 示例 8: HTML 标签移除
        Console.WriteLine("\n8. HTML 标签移除:");
        var html = "<div>Hello <b>World</b></div>";
        Console.WriteLine($"   移除标签: {StringHelper.RemoveHtmlTags(html)}");

        // 示例 9: 随机字符串生成
        Console.WriteLine("\n9. 随机字符串生成:");
        var randomStr = StringHelper.GenerateRandomString(16);
        Console.WriteLine($"   随机字符串: {randomStr}");

        // 示例 10: 哈希计算
        Console.WriteLine("\n10. 哈希计算:");
        var hashText = "Hello, World!";
        Console.WriteLine($"   MD5: {StringHelper.Md5Hash(hashText)}");
        Console.WriteLine($"   SHA-256: {StringHelper.Sha256Hash(hashText)}");
    }
}

