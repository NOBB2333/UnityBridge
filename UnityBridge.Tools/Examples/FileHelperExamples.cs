using UnityBridge.Tools;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// FileHelper 使用示例
/// </summary>
public static class FileHelperExamples
{
    public static void Run()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "UnityBridgeExamples");
        var testFile = Path.Combine(testDir, "test.txt");
        
        try
        {
            // 示例 1: 文件读写
            Console.WriteLine("1. 文件读写:");
            FileHelper.WriteFile(testFile, "Hello, World!\n这是测试内容");
            var content = FileHelper.ReadFile(testFile);
            Console.WriteLine($"   读取内容: {content.Trim()}");

            // 示例 2: 逐行读取
            Console.WriteLine("\n2. 逐行读取:");
            var lines = FileHelper.ReadFileLines(testFile);
            Console.WriteLine($"   共 {lines.Count} 行:");
            foreach (var line in lines)
            {
                Console.WriteLine($"   - {line}");
            }

            // 示例 3: JSON 文件操作
            Console.WriteLine("\n3. JSON 文件操作:");
            var jsonFile = Path.Combine(testDir, "data.json");
            var data = new Dictionary<string, object>
            {
                ["name"] = "张三",
                ["age"] = 25,
                ["city"] = "北京"
            };
            FileHelper.WriteJson(jsonFile, data);
            var loaded = FileHelper.ReadJson<Dictionary<string, object>>(jsonFile);
            Console.WriteLine($"   读取 JSON: {loaded?["name"]}");

            // 示例 4: CSV 文件操作
            Console.WriteLine("\n4. CSV 文件操作:");
            var csvFile = Path.Combine(testDir, "data.csv");
            var csvData = new List<Dictionary<string, string>>
            {
                new() { ["name"] = "张三", ["age"] = "25" },
                new() { ["name"] = "李四", ["age"] = "30" }
            };
            FileHelper.WriteCsv(csvFile, csvData);
            var csvRead = FileHelper.ReadCsv(csvFile);
            Console.WriteLine($"   读取 CSV: {csvRead.Count} 条记录");

            // 示例 5: 文件信息
            Console.WriteLine("\n5. 文件信息:");
            var info = FileHelper.GetFileInfo(testFile);
            Console.WriteLine($"   大小: {info["size"]} 字节");
            Console.WriteLine($"   扩展名: {info["extension"]}");

            // 示例 6: 编码检测
            Console.WriteLine("\n6. 编码检测:");
            var encoding = FileHelper.DetectFileEncoding(testFile);
            Console.WriteLine($"   文件编码: {encoding}");

            // 示例 7: 压缩文件
            Console.WriteLine("\n7. 压缩文件:");
            var zipFile = Path.Combine(testDir, "archive.zip");
            FileHelper.ZipFiles(new[] { testFile, jsonFile }, zipFile);
            Console.WriteLine($"   已创建压缩文件: {zipFile}");

            // 清理
            FileHelper.DeleteFile(testFile);
            FileHelper.DeleteFile(jsonFile);
            FileHelper.DeleteFile(csvFile);
            FileHelper.DeleteFile(zipFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   错误: {ex.Message}");
        }
    }
}

