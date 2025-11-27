using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Globalization;

namespace UnityBridge.Tools;

/// <summary>
/// 字符串常用处理工具，覆盖清洗、转换、校验以及加解密等场景。
/// </summary>
public static class StringHelper
{
    private static readonly Regex EmailRegex = new Regex(@"(?i)\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new Regex(@"1[3-9]\d{9}", RegexOptions.Compiled);
    private static readonly Regex IdCardRegex = new Regex(@"^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$", RegexOptions.Compiled);
    private static readonly Regex HtmlRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WordRegex = new Regex(@"\b\w+\b", RegexOptions.Compiled);
    private static readonly Regex ChineseRegex = new Regex(@"[\u4e00-\u9fff]", RegexOptions.Compiled);

    /// <summary>
    /// 去除所有空格字符。
    /// </summary>
    public static string RemoveSpaces(string text)
    {
        return text.Replace(" ", "");
    }

    /// <summary>
    /// 合并多余空格，仅保留单个空格。
    /// </summary>
    public static string RemoveExtraSpaces(string text)
    {
        return string.Join(" ", text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// 反转字符串。
    /// </summary>
    public static string ReverseString(string text)
    {
        var charArray = text.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    /// <summary>
    /// 将每个单词首字母大写。
    /// </summary>
    public static string CapitalizeWords(string text)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
    }

    /// <summary>
    /// 将分隔符格式字符串转换为小驼峰。
    /// </summary>
    public static string ToCamelCase(string text, char separator)
    {
        var words = text.Split(separator);
        if (words.Length == 0) return string.Empty;

        var result = new StringBuilder(words[0].ToLower());
        for (int i = 1; i < words.Length; i++)
        {
            var word = words[i];
            if (word.Length > 0)
            {
                result.Append(char.ToUpper(word[0]));
                result.Append(word.Substring(1));
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// 将驼峰名称转换为下划线命名。
    /// </summary>
    public static string ToSnakeCase(string text)
    {
        var snake = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (char.IsUpper(ch) && i > 0)
            {
                snake.Append('_');
            }
            snake.Append(char.ToLower(ch));
        }
        return snake.ToString();
    }

    /// <summary>
    /// 提取字符串中的所有数字片段。
    /// </summary>
    public static List<string> ExtractNumbers(string text)
    {
        var matches = Regex.Matches(text, @"\d+");
        return matches.Select(m => m.Value).ToList();
    }

    /// <summary>
    /// 提取字符串中的邮箱地址。
    /// </summary>
    public static List<string> ExtractEmails(string text)
    {
        var matches = EmailRegex.Matches(text);
        return matches.Select(m => m.Value).ToList();
    }

    /// <summary>
    /// 提取字符串中的手机号码。
    /// </summary>
    public static List<string> ExtractPhones(string text)
    {
        var matches = PhoneRegex.Matches(text);
        return matches.Select(m => m.Value).ToList();
    }

    /// <summary>
    /// 判断字符串是否为邮箱。
    /// </summary>
    public static bool IsEmail(string email)
    {
        return EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// 判断字符串是否为手机号。
    /// </summary>
    public static bool IsPhone(string phone)
    {
        return PhoneRegex.IsMatch(phone);
    }

    /// <summary>
    /// 判断字符串是否为身份证号。
    /// </summary>
    public static bool IsIdCard(string idCard)
    {
        return IdCardRegex.IsMatch(idCard);
    }

    /// <summary>
    /// 脱敏处理，保留首尾指定字符数量。
    /// </summary>
    public static string MaskSensitiveInfo(string text, char maskChar, int keepStart, int keepEnd)
    {
        if (text.Length <= keepStart + keepEnd)
        {
            return new string(maskChar, text.Length);
        }
        return text.Substring(0, keepStart) +
               new string(maskChar, text.Length - keepStart - keepEnd) +
               text.Substring(text.Length - keepEnd);
    }

    /// <summary>
    /// 超出长度时截断并拼接后缀。
    /// </summary>
    public static string TruncateString(string text, int maxLength, string suffix)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }
        int end = Math.Max(0, maxLength - suffix.Length);
        return text.Substring(0, end) + suffix;
    }

    /// <summary>
    /// 统计英文单词数量。
    /// </summary>
    public static int CountWords(string text)
    {
        return WordRegex.Matches(text).Count;
    }

    /// <summary>
    /// 统计中文字符数量。
    /// </summary>
    public static int CountChineseChars(string text)
    {
        return ChineseRegex.Matches(text).Count;
    }

    /// <summary>
    /// 移除 HTML 标签。
    /// </summary>
    public static string RemoveHtmlTags(string text)
    {
        return HtmlRegex.Replace(text, "");
    }

    /// <summary>
    /// 移除特殊字符，可选择保留自定义字符集。
    /// </summary>
    public static string RemoveSpecialChars(string text, string? keepChars = null)
    {
        string pattern;
        if (!string.IsNullOrEmpty(keepChars))
        {
            pattern = $"[^a-zA-Z0-9\u4e00-\u9fff{Regex.Escape(keepChars)}]";
        }
        else
        {
            pattern = "[^a-zA-Z0-9\u4e00-\u9fff]";
        }
        return Regex.Replace(text, pattern, "");
    }

    /// <summary>
    /// 以友好格式展示文件大小。
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        return CalcMemoryHelper.FormatBytes(bytes);
    }

    /// <summary>
    /// 生成指定长度的随机字符串，可自定义字符集。
    /// </summary>
    public static string GenerateRandomString(int length, string? chars = null)
    {
        const string defaultChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var charSet = chars ?? defaultChars;
        var result = new char[length];
        var random = new Random();
        for (int i = 0; i < length; i++)
        {
            result[i] = charSet[random.Next(charSet.Length)];
        }
        return new string(result);
    }

    /// <summary>
    /// 计算字符串 MD5。
    /// </summary>
    public static string Md5Hash(string text)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = md5.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 计算字符串 SHA-256。
    /// </summary>
    public static string Sha256Hash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Base64 编码。
    /// </summary>
    public static string Base64Encode(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Base64 解码。
    /// </summary>
    public static string Base64Decode(string text)
    {
        var bytes = Convert.FromBase64String(text.Trim());
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// 查找子串在文本中的所有起始位置。
    /// </summary>
    public static List<int> FindAllSubstrings(string text, string substring)
    {
        var positions = new List<int>();
        int start = 0;
        while ((start = text.IndexOf(substring, start, StringComparison.Ordinal)) != -1)
        {
            positions.Add(start);
            start += 1; // Rust implementation advances by 1, allowing overlaps? Rust: start += pos + 1. Wait, Rust find returns index relative to slice.
            // Rust:
            // while let Some(pos) = text[start..].find(substring) {
            //     positions.push(start + pos);
            //     start += pos + 1;
            // }
            // If text="aaa", sub="aa".
            // 1. start=0. find "aa" -> 0. push 0. start = 0 + 0 + 1 = 1.
            // 2. text[1..] = "aa". find "aa" -> 0. push 1+0 = 1. start = 1 + 0 + 1 = 2.
            // 3. text[2..] = "a". find "aa" -> None.
            // Result: [0, 1]. Overlapping is allowed in Rust implementation logic?
            // Wait, Rust `find` finds the first occurrence.
            // If I use `IndexOf` in C#, it finds first occurrence.
            // To match Rust logic `start += pos + 1`, I should do `start += 1`?
            // No, `start += pos + 1` means it advances past the *start* of the match by 1?
            // No, `pos` is relative to `text[start..]`.
            // So `start` becomes `start + pos + 1`.
            // This means it skips the first character of the match and continues searching.
            // So yes, it allows overlapping matches if the match length > 1.
            // Example: "aaaa", "aa".
            // 1. start=0. find at 0. push 0. start becomes 1.
            // 2. start=1. "aaa". find at 0 (absolute 1). push 1. start becomes 2.
            // 3. start=2. "aa". find at 0 (absolute 2). push 2. start becomes 3.
            // 4. start=3. "a". find None.
            // Result: 0, 1, 2.
            // C# `IndexOf` returns absolute index.
            // So `start` should be `index + 1`.
        }
        return positions;
    }

    /// <summary>
    /// 批量替换多个子串。
    /// </summary>
    public static string ReplaceMultiple(string text, Dictionary<string, string> replacements)
    {
        var result = new StringBuilder(text);
        foreach (var kvp in replacements)
        {
            result.Replace(kvp.Key, kvp.Value);
        }
        return result.ToString();
    }

    /// <summary>
    /// 按固定长度切分字符串。
    /// </summary>
    public static List<string> SplitByLength(string text, int length)
    {
        var result = new List<string>();
        for (int i = 0; i < text.Length; i += length)
        {
            result.Add(text.Substring(i, Math.Min(length, text.Length - i)));
        }
        return result;
    }
}
