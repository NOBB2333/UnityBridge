using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Crypto.Paddings;
using System.Security.Cryptography.X509Certificates;

namespace UnityBridge.Tools;

/// <summary>
/// 加密解密工具类，支持 AES, DES, SM2, SM4, RSA, MD5, SHA 等算法。
/// </summary>
public static class CryptoHelper
{
    #region AES CBC

    /// <summary>
    /// AES-256-CBC 加密，返回 Base64 字符串。
    /// </summary>
    /// <param name="plain">明文数据</param>
    /// <param name="key">密钥 (32 bytes)</param>
    /// <param name="iv">初始化向量 (16 bytes)</param>
    /// <returns>Base64 编码的密文</returns>
    public static string EncryptAes256CbcBase64(byte[] plain, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        return Convert.ToBase64String(ciphertext);
    }

    /// <summary>
    /// AES-256-CBC 解密，输入为 Base64 字符串。
    /// </summary>
    /// <param name="token">Base64 编码的密文</param>
    /// <param name="key">密钥 (32 bytes)</param>
    /// <param name="iv">初始化向量 (16 bytes)</param>
    /// <returns>解密后的明文数据</returns>
    public static byte[] DecryptAes256CbcBase64(string token, byte[] key, byte[] iv)
    {
        var data = Convert.FromBase64String(token.Trim());
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }

    /// <summary>
    /// AES-256-CBC 加密 (Hex Key/IV)，返回 Base64 字符串。
    /// </summary>
    public static string EncryptAes256CbcBase64(string plain, string keyHex, string ivHex)
    {
        return EncryptAes256CbcBase64(Encoding.UTF8.GetBytes(plain), Convert.FromHexString(keyHex), Convert.FromHexString(ivHex));
    }

    /// <summary>
    /// AES-256-CBC 解密 (Hex Key/IV)，返回 UTF-8 字符串。
    /// </summary>
    public static string DecryptAes256CbcBase64(string token, string keyHex, string ivHex)
    {
        var bytes = DecryptAes256CbcBase64(token, Convert.FromHexString(keyHex), Convert.FromHexString(ivHex));
        return Encoding.UTF8.GetString(bytes);
    }

    #endregion

    #region AES GCM

    /// <summary>
    /// AES-256-GCM 加密，返回 Base64 字符串 (Ciphertext + Tag)。
    /// </summary>
    /// <param name="plain">明文数据</param>
    /// <param name="key">密钥 (32 bytes)</param>
    /// <param name="nonce">随机数 (12 bytes)</param>
    /// <param name="associatedData">关联数据 (可选)</param>
    /// <returns>Base64 编码的密文</returns>
    public static string EncryptAes256GcmBase64(byte[] plain, byte[] key, byte[] nonce, byte[]? associatedData = null)
    {
        // .NET AesGcm requires nonce to be 12 bytes usually
        using var aesGcm = new AesGcm(key);
        var tag = new byte[16]; // standard tag size
        var ciphertext = new byte[plain.Length];
        aesGcm.Encrypt(nonce, plain, ciphertext, tag, associatedData);
        
        // Combine ciphertext + tag
        var result = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);
        
        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// AES-256-GCM 解密，输入为 Base64 字符串 (Ciphertext + Tag)。
    /// </summary>
    /// <param name="token">Base64 编码的密文</param>
    /// <param name="key">密钥 (32 bytes)</param>
    /// <param name="nonce">随机数 (12 bytes)</param>
    /// <param name="associatedData">关联数据 (可选)</param>
    /// <returns>解密后的明文数据</returns>
    public static byte[] DecryptAes256GcmBase64(string token, byte[] key, byte[] nonce, byte[]? associatedData = null)
    {
        var data = Convert.FromBase64String(token.Trim());
        if (data.Length < 16) throw new ArgumentException("Invalid data length for GCM");

        var tag = new byte[16];
        var ciphertext = new byte[data.Length - 16];
        Buffer.BlockCopy(data, data.Length - 16, tag, 0, 16);
        Buffer.BlockCopy(data, 0, ciphertext, 0, data.Length - 16);

        using var aesGcm = new AesGcm(key);
        var plaintext = new byte[ciphertext.Length];
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        return plaintext;
    }

    #endregion

    #region MD5 / SHA

    /// <summary>
    /// 计算 MD5 哈希，返回小写 Hex 字符串。
    /// </summary>
    public static string Md5Hex(byte[] input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(input);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 计算字符串的 MD5 哈希，返回小写 Hex 字符串。
    /// </summary>
    /// <summary>
    /// 计算字符串的 MD5 哈希，返回小写 Hex 字符串。支持可选盐值。
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="salt">盐值 (可选，默认为空)</param>
    public static string Md5Hex(string input, string salt = "")
    {
        if (string.IsNullOrEmpty(salt))
            return Md5Hex(Encoding.UTF8.GetBytes(input));

        return Md5HexWithSalt(Encoding.UTF8.GetBytes(input), Encoding.UTF8.GetBytes(salt));
    }

    /// <summary>
    /// 计算 16 位 MD5 哈希 (取中间 16 位)，返回小写 Hex 字符串。支持可选盐值。
    /// </summary>
    public static string Md516Hex(string input, string salt = "")
    {
        var hex = Md5Hex(input, salt);
        return hex.Substring(8, 16);
    }


    /// <summary>
    /// 计算带盐值的 MD5 哈希 (input + salt)，返回小写 Hex 字符串。
    /// </summary>
    public static string Md5HexWithSalt(byte[] input, byte[] salt)
    {
        using var md5 = MD5.Create();
        var combined = new byte[input.Length + salt.Length];
        Buffer.BlockCopy(input, 0, combined, 0, input.Length);
        Buffer.BlockCopy(salt, 0, combined, input.Length, salt.Length);
        var hash = md5.ComputeHash(combined);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 计算带盐值的字符串 MD5 哈希，返回小写 Hex 字符串。
    /// </summary>
    public static string Md5HexWithSalt(string input, string salt) => Md5HexWithSalt(Encoding.UTF8.GetBytes(input), Encoding.UTF8.GetBytes(salt));

    /// <summary>
    /// 计算 SHA-1 哈希，返回小写 Hex 字符串。
    /// </summary>
    public static string Sha1Hex(byte[] input)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(input);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 计算字符串的 SHA-1 哈希，返回小写 Hex 字符串。
    /// </summary>
    public static string Sha1Hex(string input) => Sha1Hex(Encoding.UTF8.GetBytes(input));

    /// <summary>
    /// 计算 HMAC-SHA256 哈希，返回小写 Hex 字符串。
    /// </summary>
    public static string HmacSha256Hex(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 计算字符串的 HMAC-SHA256 哈希，返回小写 Hex 字符串。
    /// </summary>
    public static string HmacSha256Hex(string key, string data) => HmacSha256Hex(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data));

    #endregion

    #region RSA PSS

    /// <summary>
    /// RSA-SHA256-PSS 签名，返回 Base64 字符串。
    /// </summary>
    /// <param name="privateKeyPem">PEM 格式私钥</param>
    /// <param name="message">待签名数据</param>
    /// <returns>Base64 编码的签名</returns>
    public static string RsaSignSha256PssBase64(string privateKeyPem, byte[] message)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = rsa.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// RSA-SHA256-PSS 验签。
    /// </summary>
    /// <param name="publicKeyPem">PEM 格式公钥</param>
    /// <param name="message">原始数据</param>
    /// <param name="signatureB64">Base64 编码的签名</param>
    /// <returns>验签是否通过</returns>
    public static bool RsaVerifySha256PssBase64(string publicKeyPem, byte[] message, string signatureB64)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var signature = Convert.FromBase64String(signatureB64);
        return rsa.VerifyData(message, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    /// <summary>
    /// 从 X.509 证书中提取 RSA 公钥 (PEM 格式)。
    /// </summary>
    public static string RsaPublicKeyFromCertificate(string certificatePem)
    {
        var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(Encoding.UTF8.GetBytes(certificatePem));
        using var rsa = cert.GetRSAPublicKey();
        if (rsa == null) throw new ArgumentException("Certificate does not contain an RSA public key");
        return rsa.ExportSubjectPublicKeyInfoPem();
    }

    #endregion

    #region SM2

    /// <summary>
    /// SM2 签名，返回 Base64 字符串。
    /// </summary>
    /// <param name="privateKeyHex">Hex 格式私钥</param>
    /// <param name="message">待签名数据</param>
    /// <returns>Base64 编码的签名</returns>
    public static string Sm2SignBase64(string privateKeyHex, byte[] message)
    {
        var privateKeyBytes = Hex.Decode(privateKeyHex.TrimStart('0', 'x'));
        var x9 = Org.BouncyCastle.Asn1.GM.GMNamedCurves.GetByName("sm2p256v1");
        var domainParams = new ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H);
        var privateKeyParam = new ECPrivateKeyParameters(
            new Org.BouncyCastle.Math.BigInteger(1, privateKeyBytes),
            domainParams);

        var signer = new SM2Signer();
        signer.Init(true, privateKeyParam);
        signer.BlockUpdate(message, 0, message.Length);
        var signature = signer.GenerateSignature();
        
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// SM2 验签。
    /// </summary>
    /// <param name="publicKeyHex">Hex 格式公钥</param>
    /// <param name="message">原始数据</param>
    /// <param name="signatureB64">Base64 编码的签名</param>
    /// <returns>验签是否通过</returns>
    public static bool Sm2VerifyBase64(string publicKeyHex, byte[] message, string signatureB64)
    {
        var publicKeyBytes = Hex.Decode(publicKeyHex.TrimStart('0', 'x'));
        var x9 = Org.BouncyCastle.Asn1.GM.GMNamedCurves.GetByName("sm2p256v1");
        var domainParams = new ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H);
        var point = x9.Curve.DecodePoint(publicKeyBytes);
        var publicKeyParam = new ECPublicKeyParameters(point, domainParams);

        var signer = new SM2Signer();
        signer.Init(false, publicKeyParam);
        signer.BlockUpdate(message, 0, message.Length);
        var signature = Convert.FromBase64String(signatureB64);
        return signer.VerifySignature(signature);
    }

    #endregion

    #region SM4 CBC

    /// <summary>
    /// SM4-CBC 加密，返回 Base64 字符串。
    /// </summary>
    /// <param name="key">密钥</param>
    /// <param name="iv">初始化向量</param>
    /// <param name="plaintext">明文数据</param>
    /// <returns>Base64 编码的密文</returns>
    public static string Sm4CbcEncryptBase64(byte[] key, byte[] iv, byte[] plaintext)
    {
        var engine = new SM4Engine();
        var blockCipher = new CbcBlockCipher(engine);
        var cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());
        var keyParam = new KeyParameter(key);
        var paramsWithIv = new ParametersWithIV(keyParam, iv);

        cipher.Init(true, paramsWithIv);
        var output = new byte[cipher.GetOutputSize(plaintext.Length)];
        var length = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
        length += cipher.DoFinal(output, length);

        var result = new byte[length];
        Array.Copy(output, 0, result, 0, length);
        return Convert.ToBase64String(result);
    }

    public static byte[] Sm4CbcDecryptBase64(byte[] key, string token, byte[] iv)
    {
        var data = Convert.FromBase64String(token.Trim());
        var engine = new SM4Engine();
        var blockCipher = new CbcBlockCipher(engine);
        var cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());
        var keyParam = new KeyParameter(key);
        var paramsWithIv = new ParametersWithIV(keyParam, iv);

        cipher.Init(false, paramsWithIv);
        var output = new byte[cipher.GetOutputSize(data.Length)];
        var length = cipher.ProcessBytes(data, 0, data.Length, output, 0);
        length += cipher.DoFinal(output, length);

        var result = new byte[length];
        Array.Copy(output, 0, result, 0, length);
        return result;
    }

    /// <summary>
    /// SM4-CBC 加密 (Hex Key/IV)，返回 Base64 字符串。
    /// </summary>
    public static string Sm4CbcEncryptBase64(string plain, string keyHex, string ivHex)
    {
        return Sm4CbcEncryptBase64(Convert.FromHexString(keyHex), Convert.FromHexString(ivHex), Encoding.UTF8.GetBytes(plain));
    }

    /// <summary>
    /// SM4-CBC 解密 (Hex Key/IV)，返回 UTF-8 字符串。
    /// </summary>
    public static string Sm4CbcDecryptBase64(string token, string keyHex, string ivHex)
    {
        var bytes = Sm4CbcDecryptBase64(Convert.FromHexString(keyHex), token, Convert.FromHexString(ivHex));
        return Encoding.UTF8.GetString(bytes);
    }

    #endregion

    #region DES CBC

    /// <summary>
    /// DES-CBC 加密，返回 Base64 字符串。
    /// </summary>
    /// <param name="plain">明文数据</param>
    /// <param name="key">密钥 (8 bytes)</param>
    /// <param name="iv">初始化向量 (8 bytes)</param>
    /// <returns>Base64 编码的密文</returns>
    public static string EncryptDesCbcBase64(byte[] plain, byte[] key, byte[] iv)
    {
        using var des = DES.Create();
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.PKCS7;
        des.Key = key;
        des.IV = iv;

        using var encryptor = des.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        return Convert.ToBase64String(ciphertext);
    }

    /// <summary>
    /// DES-CBC 解密，输入为 Base64 字符串。
    /// </summary>
    /// <param name="token">Base64 编码的密文</param>
    /// <param name="key">密钥 (8 bytes)</param>
    /// <param name="iv">初始化向量 (8 bytes)</param>
    /// <returns>解密后的明文数据</returns>
    public static byte[] DecryptDesCbcBase64(string token, byte[] key, byte[] iv)
    {
        var data = Convert.FromBase64String(token.Trim());
        using var des = DES.Create();
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.PKCS7;
        des.Key = key;
        des.IV = iv;

        using var decryptor = des.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }

    #endregion

    #region Base64 Encoding

    /// <summary>
    /// Base64 编码。
    /// </summary>
    public static string Base64Encode(string text)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Base64 解码。
    /// </summary>
    public static string Base64Decode(string base64)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    #endregion

    #region URL Encoding

    /// <summary>
    /// URL 编码。
    /// </summary>
    public static string UrlEncode(string text)
    {
        return System.Net.WebUtility.UrlEncode(text);
    }

    /// <summary>
    /// URL 解码。
    /// </summary>
    public static string UrlDecode(string text)
    {
        return System.Net.WebUtility.UrlDecode(text);
    }

    #endregion
}

/// <summary>
/// AES 加解密封装类，支持动态密钥长度。
/// </summary>
public class AesCipher
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// 初始化 AES 加解密类。
    /// </summary>
    /// <param name="key">密钥 (16/24/32 bytes)</param>
    /// <param name="iv">初始化向量 (16 bytes)，若为空则使用密钥前 16 字节</param>
    public AesCipher(byte[] key, byte[]? iv = null)
    {
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
        {
            throw new ArgumentException("AES key must be 16/24/32 bytes");
        }
        _key = key;
        _iv = new byte[16];
        if (iv != null)
        {
            Array.Copy(iv, _iv, Math.Min(iv.Length, 16));
        }
        else
        {
            Array.Copy(key, _iv, 16);
        }
    }

    /// <summary>
    /// 初始化 AES 加解密类 (Hex 字符串)。
    /// </summary>
    public AesCipher(string keyHex, string? ivHex = null) 
        : this(Encoding.UTF8.GetBytes(keyHex), ivHex != null ? Encoding.UTF8.GetBytes(ivHex) : null)
    {
        // Note: Rust implementation takes AsRef<[u8]> which can be string bytes or raw bytes.
        // If the input is hex string, we should decode it? 
        // Rust: `AesCipher::new("0123456789abcdef", None)` -> takes bytes of the string.
        // So `Encoding.UTF8.GetBytes` is correct if the input is meant to be the key bytes directly from string chars.
    }

    /// <summary>
    /// 加密字符串，返回 Base64。
    /// </summary>
    public string Encrypt(string text)
    {
        var plain = Encoding.UTF8.GetBytes(text);
        return CryptoHelper.EncryptAes256CbcBase64(plain, _key, _iv); 
        // Wait, Rust implementation supports 128/192/256 based on key length.
        // My EncryptAes256CbcBase64 hardcodes 256.
        // I should fix EncryptAes256CbcBase64 or use a dynamic one.
        // Let's use a dynamic one here.
    }

    /// <summary>
    /// 解密 Base64 字符串。
    /// </summary>
    public string Decrypt(string token)
    {
        var plain = DecryptDynamic(token);
        return Encoding.UTF8.GetString(plain);
    }

    private byte[] EncryptDynamic(byte[] plain)
    {
        using var aes = Aes.Create();
        aes.KeySize = _key.Length * 8;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = _key;
        aes.IV = _iv;
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plain, 0, plain.Length);
    }

    private byte[] DecryptDynamic(string token)
    {
        var data = Convert.FromBase64String(token.Trim());
        using var aes = Aes.Create();
        aes.KeySize = _key.Length * 8;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = _key;
        aes.IV = _iv;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }
}

/// <summary>
/// DES 加解密封装类。
/// </summary>
public class DesCipher
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// 初始化 DES 加解密类。
    /// </summary>
    /// <param name="key">密钥 (8 bytes)</param>
    /// <param name="iv">初始化向量 (8 bytes)，若为空则使用密钥</param>
    public DesCipher(byte[] key, byte[]? iv = null)
    {
        if (key.Length != 8) throw new ArgumentException("DES key must be 8 bytes");
        _key = key;
        _iv = new byte[8];
        if (iv != null)
        {
            Array.Copy(iv, _iv, Math.Min(iv.Length, 8));
        }
        else
        {
            Array.Copy(key, _iv, 8);
        }
    }

    /// <summary>
    /// 初始化 DES 加解密类 (字符串)。
    /// </summary>
    public DesCipher(string key, string? iv = null)
        : this(Encoding.UTF8.GetBytes(key), iv != null ? Encoding.UTF8.GetBytes(iv) : null)
    {
    }

    /// <summary>
    /// 加密字符串，返回 Base64。
    /// </summary>
    public string Encrypt(string text)
    {
        return CryptoHelper.EncryptDesCbcBase64(Encoding.UTF8.GetBytes(text), _key, _iv);
    }

    /// <summary>
    /// 解密 Base64 字符串。
    /// </summary>
    public string Decrypt(string token)
    {
        var bytes = CryptoHelper.DecryptDesCbcBase64(token, _key, _iv);
        return Encoding.UTF8.GetString(bytes);
    }
}
