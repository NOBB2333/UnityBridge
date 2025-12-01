using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using UnityBridge.Tools;

namespace UnityBridge.Helpers;

/// <summary>
/// 试用期管理类，用于检测试用期和验证激活 key
/// </summary>
public static class TrialManager
{
    private const string ConfigFileName = "trial.dat"; // 二进制加密文件
    private const int TrialDays = 30; // 试用期30天
    
    // AES 加密密钥和 IV（用于加密试用期信息）
    // 注意：实际应用中应该使用更安全的密钥管理方式；此处使用你提供的字符串作为种子，
    // 再通过哈希派生出真正符合长度要求的 Key/IV，避免“Specified key is not a valid size for this algorithm”异常。
    // 原始密钥字符串（保持不变）：
    // 注意：实际应用中应该使用更安全的密钥管理方式；此处仅为示例：
    // - AES Key: 32 bytes
    // - AES IV : 16 bytes
    private const string AesKeySeed       = "UnityBridgeTrialKey1234567890123456";
    private const string AesIvSeed        = "UnityBridgeIV123";
    private const string KeyGenSecretSeed = "UnityBridgeKeyGenSecret1234567890123456";

    // 实际用于 AES/HMAC 的二进制密钥（从种子派生而来，长度符合算法要求：Key 32 字节，IV 16 字节）
    private static readonly byte[] AesKey       = DeriveKey(AesKeySeed, 32);
    private static readonly byte[] AesIv        = DeriveKey(AesIvSeed, 16);
    private static readonly byte[] KeyGenSecret = DeriveKey(KeyGenSecretSeed, 32);

    /// <summary>
    /// 从字符串种子派生固定长度的密钥/IV。
    /// 做法：对种子做 SHA256，再截取前 size 字节；若 size 大于 32，则循环填充。
    /// </summary>
    private static byte[] DeriveKey(string seed, int size)
    {
        var seedBytes = Encoding.UTF8.GetBytes(seed);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(seedBytes); // 32 bytes

        var result = new byte[size];
        for (int i = 0; i < size; i++)
        {
            result[i] = hash[i % hash.Length];
        }

        return result;
    }

    /// <summary>
    /// 生成当前机器的硬件指纹（机器码），用于离线授权绑定。
    /// 这里使用若干硬件信息 + MD5 摘要，外部只看到一串 32 位 Hex。
    /// </summary>
    private static string GetHardwareId()
    {
        var markers = new List<string>();
        markers.AddRange(GetWmiHardwareMarkers());

        if (markers.Count == 0)
        {
            markers.AddRange(GetFallbackHardwareMarkers());
        }

        var sb = new StringBuilder();
        foreach (var marker in markers)
        {
            if (string.IsNullOrWhiteSpace(marker))
                continue;

            if (sb.Length > 0)
            {
                sb.Append('|');
            }

            sb.Append(marker.Trim());
        }

        if (sb.Length == 0)
        {
            sb.Append(Environment.MachineName);
        }

        var rawBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var md5 = CryptoHelper.Md5Hex(rawBytes);
        return md5.ToUpperInvariant(); // 固定 32 字符长度
    }

    private static IEnumerable<string> GetWmiHardwareMarkers()
    {
        if (!OperatingSystem.IsWindows())
            yield break;

        var targets = new (string ClassName, string Property)[]
        {
            ("Win32_ComputerSystemProduct", "UUID"),
            ("Win32_BaseBoard", "SerialNumber"),
            ("Win32_BIOS", "SerialNumber"),
            ("Win32_Processor", "ProcessorId"),
            ("Win32_DiskDrive", "SerialNumber")
        };

        foreach (var (className, property) in targets)
        {
            var value = TryGetWmiProperty(className, property);
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value!;
            }
        }
    }

    private static string? TryGetWmiProperty(string className, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {className}");
            using var results = searcher.Get();
            foreach (var obj in results)
            {
                if (obj is not ManagementObject managementObject)
                    continue;

                var value = managementObject[property]?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (value.Equals("To Be Filled By O.E.M.", StringComparison.OrdinalIgnoreCase))
                    continue;

                return value.Trim();
            }
        }
        catch
        {
            // 忽略无法访问 WMI 的情况
        }

        return null;
    }

    private static IEnumerable<string> GetFallbackHardwareMarkers()
    {
        yield return Environment.MachineName;
        yield return Environment.SystemDirectory;
        yield return Environment.ProcessorCount.ToString();

        var mac = GetPrimaryMacAddress();
        if (!string.IsNullOrWhiteSpace(mac))
        {
            yield return mac;
        }
    }

    private static string? GetPrimaryMacAddress()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                var macBytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (macBytes.Length == 0)
                    continue;

                return BitConverter.ToString(macBytes).Replace("-", "");
            }
        }
        catch
        {
            // 忽略获取网卡信息失败的情况
        }

        return null;
    }

    /// <summary>
    /// 提供给用户拷贝的“机器码”，本质就是硬件指纹的 Hex。
    /// </summary>
    public static string GetMachineCode() => GetHardwareId();

    /// <summary>
    /// 获取配置文件路径（使用运行目录）
    /// </summary>
    private static string GetConfigFilePath()
    {
        // 使用运行目录，而不是源码目录
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(exeDir, "Const");
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }
        return Path.Combine(configDir, ConfigFileName);
    }

    /// <summary>
    /// 试用期配置数据
    /// </summary>
    private class TrialData
    {
        public DateTime FirstRunTime { get; set; }
        public DateTime ExpireTime { get; set; }
        public bool IsActivated { get; set; }
    }

    /// <summary>
    /// 检查试用期状态，如果过期则要求输入 key
    /// </summary>
    /// <returns>true 表示可以继续使用，false 表示已过期且未激活</returns>
    public static bool CheckTrialStatus()
    {
        var configPath = GetConfigFilePath();
        var data = LoadConfig(configPath);
        
        // 判断是否为首次运行：配置文件不存在
        bool isFirstRun = !File.Exists(configPath);
        
        // 如果是首次运行，初始化试用期
        if (isFirstRun)
        {
            data.FirstRunTime = DateTime.Now;
            data.ExpireTime = DateTime.Now.AddDays(TrialDays);
            data.IsActivated = false;
            SaveConfig(configPath, data);
            Console.WriteLine("========================================");
            Console.WriteLine("欢迎使用 UnityBridge！");
            Console.WriteLine($"您有 {TrialDays} 天的试用期。");
            Console.WriteLine($"试用期开始时间：{data.FirstRunTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"试用期到期时间：{data.ExpireTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("========================================\n");
            return true;
        }

        // 如果已激活，直接通过
        if (data.IsActivated)
        {
            return true;
        }

        // 检查是否过期
        if (DateTime.Now > data.ExpireTime)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("试用期已过期！");
            Console.WriteLine($"试用期到期时间：{data.ExpireTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("========================================\n");
            
            // 允许用户多次尝试输入 key
            int maxAttempts = 3;
            int attempts = 0;
            
            while (attempts < maxAttempts)
            {
                Console.Write($"请输入激活 key（剩余尝试次数：{maxAttempts - attempts}，输入 'exit' 退出）: ");
                
                var inputKey = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(inputKey) || inputKey.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("程序退出。");
                    return false;
                }

                // 验证 key 并从 key 中解析延期时间（使用改进的方法）
                var newExpireTime = ValidateAndParseActivationKeyImproved(inputKey);
                if (newExpireTime.HasValue && newExpireTime.Value > DateTime.Now)
                {
                    // Key 有效，更新到期时间
                    data.ExpireTime = newExpireTime.Value;
                    data.IsActivated = true;
                    SaveConfig(configPath, data);
                    Console.WriteLine("\n========================================");
                    Console.WriteLine("激活成功！");
                    Console.WriteLine($"新的到期时间：{data.ExpireTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine("========================================\n");
                    return true;
                }
                else
                {
                    attempts++;
                    if (attempts < maxAttempts)
                    {
                        Console.WriteLine($"激活 key 无效或已过期！请检查 key 是否正确。\n");
                    }
                    else
                    {
                        Console.WriteLine("激活 key 验证失败次数过多，程序退出。");
                        return false;
                    }
                }
            }
            
            return false;
        }

        // 试用期内，显示剩余时间
        var remaining = data.ExpireTime - DateTime.Now;
        var remainingDays = remaining.Days;
        var remainingHours = remaining.Hours;
        
        if (remainingDays > 0)
        {
            Console.WriteLine($"试用期剩余 {remainingDays} 天 {remainingHours} 小时。");
        }
        else if (remainingHours > 0)
        {
            Console.WriteLine($"试用期剩余 {remainingHours} 小时。");
        }
        else
        {
            Console.WriteLine($"试用期即将到期（剩余 {remaining.Minutes} 分钟）。");
        }
        Console.WriteLine($"试用期到期时间：{data.ExpireTime:yyyy-MM-dd HH:mm:ss}\n");
        
        return true;
    }

    /// <summary>
    /// 验证激活 key 并从 key 中解析延期时间
    /// </summary>
    /// <param name="key">激活 key（Base64 编码）</param>
    /// <returns>延期后的到期时间，如果 key 无效则返回 null</returns>
    private static DateTime? ValidateAndParseActivationKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        key = key.Trim();

        try
        {
            // Key 格式：Base64 编码的加密数据
            // 加密数据包含：延期时间戳（8字节）+ HMAC-SHA256 签名（32字节）
            var encryptedData = Convert.FromBase64String(key);
            
            if (encryptedData.Length < 40) // 至少需要 8字节时间戳 + 32字节签名
                return null;

            // 使用 AES 解密 key（这里 key 本身是加密的延期信息）
            // 实际应用中，key 应该包含延期时间戳和签名
            // 为了简化，我们使用 HMAC 验证 key 的有效性，然后从 key 的哈希中推导时间
            
            // 方法1：从 key 的哈希值推导延期时间（示例实现）
            // 实际应用中应该使用更安全的方式，比如服务器验证或 RSA 签名
            
            // 计算 key 的 HMAC
            var keyHmac = CryptoHelper.HmacSha256Hex(KeyGenSecret, Encoding.UTF8.GetBytes(key));
            
            // 从 HMAC 中提取时间信息（简化实现）
            // 实际应用中，key 应该明确包含延期时间戳
            var timeBytes = new byte[8];
            for (int i = 0; i < 8 && i < keyHmac.Length / 2; i++)
            {
                var hexByte = keyHmac.Substring(i * 2, 2);
                timeBytes[i] = Convert.ToByte(hexByte, 16);
            }
            
            // 这里使用一个简化的方法：从 key 的哈希推导延期天数
            // 实际应用中，key 应该明确包含延期时间戳
            var daysToAdd = (timeBytes[0] % 365) + 30; // 延期 30-395 天
            var newExpireTime = DateTime.Now.AddDays(daysToAdd);
            
            // 验证 key 格式（检查是否包含有效的延期信息）
            // 这里可以添加更复杂的验证逻辑
            if (daysToAdd > 0 && daysToAdd < 1000) // 合理的延期范围
            {
                return newExpireTime;
            }
            
            return null;
        }
        catch (FormatException)
        {
            // Base64 格式错误
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 生成激活 key（用于测试/单机小工具，实际应该由发行方在离线授权工具中生成）
    /// 该 key 绑定当前机器的硬件指纹。
    /// </summary>
    /// <param name="daysToAdd">从当前时间起延长的天数</param>
    /// <returns>激活 key（Base64）</returns>
    public static string GenerateActivationKey(int daysToAdd)
    {
        var hardwareId = GetHardwareId();
        var expireTime = DateTime.Now.AddDays(daysToAdd);
        return GenerateActivationKeyForHardware(hardwareId, expireTime);
    }

    /// <summary>
    /// 用于“离线授权工具”的重载：根据指定的机器码和到期时间生成激活 key。
    /// 你自己的授权生成工具可以引用这段逻辑，也可以复制改造。
    /// </summary>
    /// <param name="machineCode">客户端提供的机器码（GetMachineCode 输出）</param>
    /// <param name="expireTime">授权到期时间（UTC 或本地时间需前后一致）</param>
    public static string GenerateActivationKeyForHardware(string machineCode, DateTime expireTime)
    {
        if (string.IsNullOrWhiteSpace(machineCode))
            throw new ArgumentException("machineCode is required", nameof(machineCode));

        var timeBytes = BitConverter.GetBytes(expireTime.ToBinary()); // 8 bytes

        // 机器码固定为 32 字符 Hex，这里直接按 UTF8 编码截断/填充到 32 字节
        var hwString = machineCode.Trim();
        if (hwString.Length > 32)
            hwString = hwString[..32];
        var hwBytes = Encoding.UTF8.GetBytes(hwString.PadRight(32, '0')); // 32 bytes

        // 随机填充 16 字节
        var random = new Random();
        var randomBytes = new byte[16];
        random.NextBytes(randomBytes);

        // 数据部分：时间戳(8) + 硬件ID(32) + 随机(16) = 56 字节
        var data = new byte[8 + 32 + 16];
        Array.Copy(timeBytes, 0, data, 0, 8);
        Array.Copy(hwBytes, 0, data, 8, 32);
        Array.Copy(randomBytes, 0, data, 40, 16);

        // 计算 HMAC 作为完整性校验（对 Base64(data) 做 HMAC-SHA256）
        var dataBase64 = Convert.ToBase64String(data);
        var hmacHex = CryptoHelper.HmacSha256Hex(KeyGenSecret, Encoding.UTF8.GetBytes(dataBase64));
        var hmac = Convert.FromHexString(hmacHex); // 32 bytes

        // 最终明文：data(56) + hmac(32) = 88 字节
        var finalData = new byte[data.Length + 32];
        Array.Copy(data, 0, finalData, 0, data.Length);
        Array.Copy(hmac, 0, finalData, data.Length, 32);

        // 使用 AES 加密（key 使用 KeyGenSecret，IV 使用 AesIv）
        var encrypted = CryptoHelper.EncryptAes256CbcBase64(finalData, KeyGenSecret, AesIv);
        return encrypted;
    }

    /// <summary>
    /// 改进的 key 验证：从加密的 key 中解析延期时间，并校验硬件绑定 + HMAC
    /// </summary>
    private static DateTime? ValidateAndParseActivationKeyImproved(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        try
        {
            // 解密 key
            var decrypted = CryptoHelper.DecryptAes256CbcBase64(key, KeyGenSecret, AesIv);
            
            // 至少需要：时间戳(8) + 硬件ID(32) + 随机(16) + HMAC(32) = 88 字节
            if (decrypted.Length < 88)
                return null;
            
            // 提取时间戳（前8字节）
            var timeBytes = new byte[8];
            Array.Copy(decrypted, 0, timeBytes, 0, 8);
            var expireTime = DateTime.FromBinary(BitConverter.ToInt64(timeBytes, 0));
            
            // 验证时间是否在未来
            if (expireTime <= DateTime.Now)
                return null;
            
            // 提取硬件指纹（紧跟在时间戳之后，32字节）
            var hwBytes = new byte[32];
            Array.Copy(decrypted, 8, hwBytes, 0, 32);
            var licenseHw = Encoding.UTF8.GetString(hwBytes).TrimEnd('\0', ' ');

            // 与当前机器的硬件指纹对比，不一致则认为 key 非本机授权
            var currentHw = GetHardwareId();
            if (!string.Equals(licenseHw, currentHw, StringComparison.OrdinalIgnoreCase))
                return null;

            // 验证 HMAC（数据格式：8字节时间戳 + 32字节硬件ID + 16字节随机数据 + 32字节HMAC = 88字节）
            // dataPart 为前 56 字节（时间戳 + 硬件ID + 随机）
            var dataPart = new byte[8 + 32 + 16];
            Array.Copy(decrypted, 0, dataPart, 0, dataPart.Length);

            // 计算期望的 HMAC（hex 字符串转字节）
            var dataPartBase64 = Convert.ToBase64String(dataPart);
            var expectedHmacHex = CryptoHelper.HmacSha256Hex(KeyGenSecret, Encoding.UTF8.GetBytes(dataPartBase64));
            var expectedHmac = Convert.FromHexString(expectedHmacHex);

            // 提取实际的 HMAC（后32字节）
            var actualHmac = new byte[32];
            Array.Copy(decrypted, dataPart.Length, actualHmac, 0, 32);
                
            // 安全比较 HMAC
            if (expectedHmac.Length == actualHmac.Length)
            {
                bool hmacValid = true;
                for (int i = 0; i < expectedHmac.Length; i++)
                {
                    if (expectedHmac[i] != actualHmac[i])
                    {
                        hmacValid = false;
                        break;
                    }
                }
                    
                if (hmacValid)
                {
                    return expireTime;
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 加载配置（从二进制加密文件）
    /// </summary>
    private static TrialData LoadConfig(string configPath)
    {
        try
        {
            if (File.Exists(configPath))
            {
                // 读取加密的二进制文件
                var encryptedData = File.ReadAllBytes(configPath);
                
                // 解密数据
                var decryptedBase64 = CryptoHelper.DecryptAes256CbcBase64(
                    Convert.ToBase64String(encryptedData),
                    AesKey,
                    AesIv
                );
                
                var decryptedJson = Encoding.UTF8.GetString(decryptedBase64);
                
                // 解析 JSON（临时使用，后续可以改为纯二进制格式）
                var lines = decryptedJson.Split('\n');
                var data = new TrialData();
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("FirstRunTime:"))
                    {
                        var timeStr = line.Substring("FirstRunTime:".Length).Trim();
                        if (DateTime.TryParse(timeStr, out var firstRun))
                            data.FirstRunTime = firstRun;
                    }
                    else if (line.StartsWith("ExpireTime:"))
                    {
                        var timeStr = line.Substring("ExpireTime:".Length).Trim();
                        if (DateTime.TryParse(timeStr, out var expire))
                            data.ExpireTime = expire;
                    }
                    else if (line.StartsWith("IsActivated:"))
                    {
                        var activatedStr = line.Substring("IsActivated:".Length).Trim();
                        data.IsActivated = bool.TryParse(activatedStr, out var activated) && activated;
                    }
                }
                
                return data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载配置失败: {ex.Message}");
        }

        return new TrialData();
    }

    /// <summary>
    /// 保存配置（保存为二进制加密文件）
    /// </summary>
    private static void SaveConfig(string configPath, TrialData data)
    {
        try
        {
            // 将数据序列化为简单格式
            var json = $"FirstRunTime:{data.FirstRunTime:yyyy-MM-dd HH:mm:ss}\n" +
                      $"ExpireTime:{data.ExpireTime:yyyy-MM-dd HH:mm:ss}\n" +
                      $"IsActivated:{data.IsActivated}";
            
            // 加密数据
            var encryptedBase64 = CryptoHelper.EncryptAes256CbcBase64(
                Encoding.UTF8.GetBytes(json),
                AesKey,
                AesIv
            );
            
            // 转换为二进制
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            
            // 确保目录存在
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            // 写入二进制文件
            File.WriteAllBytes(configPath, encryptedBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示试用期信息（供用户查看）
    /// </summary>
    public static void ShowTrialInfo()
    {
        var configPath = GetConfigFilePath();
        var data = LoadConfig(configPath);
        
        Console.WriteLine("\n========================================");
        Console.WriteLine("试用期信息");
        Console.WriteLine("========================================");
        
        if (!File.Exists(configPath))
        {
            Console.WriteLine("状态：未初始化");
            Console.WriteLine("提示：首次运行程序时会自动初始化试用期。");
        }
        else if (data.IsActivated)
        {
            Console.WriteLine("状态：已激活");
            Console.WriteLine($"首次运行时间：{data.FirstRunTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"到期时间：{data.ExpireTime:yyyy-MM-dd HH:mm:ss}");
            var remaining = data.ExpireTime - DateTime.Now;
            if (remaining.TotalDays > 0)
            {
                Console.WriteLine($"剩余时间：{remaining.Days} 天 {remaining.Hours} 小时");
            }
            else
            {
                Console.WriteLine("状态：永久激活");
            }
        }
        else
        {
            Console.WriteLine("状态：试用中");
            Console.WriteLine($"首次运行时间：{data.FirstRunTime:yyyy-MM-dd HH:mm:ss}");
            
            var now = DateTime.Now;
            if (now > data.ExpireTime)
            {
                Console.WriteLine($"试用期到期时间：{data.ExpireTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("状态：已过期（需要激活）");
            }
            else
            {
                var remaining = data.ExpireTime - now;
                Console.WriteLine($"试用期到期时间：{data.ExpireTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"剩余时间：{remaining.Days} 天 {remaining.Hours} 小时 {remaining.Minutes} 分钟");
            }
        }
        
        Console.WriteLine($"配置文件位置：{configPath}");
        Console.WriteLine("========================================\n");
    }
}
