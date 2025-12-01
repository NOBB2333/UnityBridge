using System.Text;
using UnityBridge.Tools;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// CryptoHelper 使用示例
/// </summary>
public static class CryptoHelperExamples
{
    public static void Run()
    {
        // 示例 1: AES-256-CBC 加密解密
        Console.WriteLine("1. AES-256-CBC 加密解密:");
        var plaintext = "Hello, World! 这是测试数据";
        var key = Convert.FromHexString("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
        var iv = Convert.FromHexString("0123456789abcdef0123456789abcdef");
        var encrypted = CryptoHelper.EncryptAes256CbcBase64(Encoding.UTF8.GetBytes(plaintext), key, iv);
        Console.WriteLine($"   加密: {encrypted}");
        var decrypted = Encoding.UTF8.GetString(CryptoHelper.DecryptAes256CbcBase64(encrypted, key, iv));
        Console.WriteLine($"   解密: {decrypted}");

        // 示例 2: MD5 哈希
        Console.WriteLine("\n2. MD5 哈希:");
        var md5Hash = CryptoHelper.Md5Hex(plaintext);
        Console.WriteLine($"   MD5: {md5Hash}");

        // 示例 3: SHA-1 哈希
        Console.WriteLine("\n3. SHA-1 哈希:");
        var sha1Hash = CryptoHelper.Sha1Hex(plaintext);
        Console.WriteLine($"   SHA-1: {sha1Hash}");

        // 示例 4: HMAC-SHA256
        Console.WriteLine("\n4. HMAC-SHA256:");
        var hmacKey = "secret-key";
        var hmacHash = CryptoHelper.HmacSha256Hex(hmacKey, plaintext);
        Console.WriteLine($"   HMAC-SHA256: {hmacHash}");

        // 示例 5: Base64 编码解码
        Console.WriteLine("\n5. Base64 编码解码:");
        var base64Encoded = CryptoHelper.Base64Encode(plaintext);
        Console.WriteLine($"   编码: {base64Encoded}");
        var base64Decoded = CryptoHelper.Base64Decode(base64Encoded);
        Console.WriteLine($"   解码: {base64Decoded}");

        // 示例 6: URL 编码解码
        Console.WriteLine("\n6. URL 编码解码:");
        var urlText = "Hello 世界";
        var urlEncoded = CryptoHelper.UrlEncode(urlText);
        Console.WriteLine($"   编码: {urlEncoded}");
        var urlDecoded = CryptoHelper.UrlDecode(urlEncoded);
        Console.WriteLine($"   解码: {urlDecoded}");
    }
}

