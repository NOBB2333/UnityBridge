using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CsvHelper;

namespace UnityBridge.Tools;

/// <summary>
/// 文件及目录操作工具，包含读写、压缩、编码检测等功能。
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// 读取整个文件内容为字符串。
    /// </summary>
    public static string ReadFile(string path) => File.ReadAllText(path);

    /// <summary>
    /// 逐行读取文件，返回行向量。
    /// </summary>
    public static List<string> ReadFileLines(string path) => File.ReadAllLines(path).ToList();

    /// <summary>
    /// 以惰性迭代器方式逐行读取文件，适合大文件。
    /// </summary>
    public static IEnumerable<string> ReadFileIter(string path) => File.ReadLines(path);

    /// <summary>
    /// 按块读取文件，返回每个块的字符串表示。
    /// </summary>
    public static List<string> ReadFileChunks(string path, int chunkSize)
    {
        var chunks = new List<string>();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var buffer = new byte[chunkSize];
        int bytesRead;
        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
        {
            chunks.Add(Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }

        return chunks;
    }

    /// <summary>
    /// 覆盖写入文件内容。
    /// </summary>
    public static void WriteFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, content);
    }

    /// <summary>
    /// 向文件追加一行内容，不存在则创建。
    /// </summary>
    public static void AppendFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.AppendAllText(path, content + Environment.NewLine);
    }

    /// <summary>
    /// 读取 JSON 文件并反序列化。
    /// </summary>
    public static T? ReadJson<T>(string path) => JsonSerializer.Deserialize<T>(ReadFile(path));

    /// <summary>
    /// 将结构体序列化为 JSON 文件。
    /// </summary>
    public static void WriteJson<T>(string path, T data)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 将 CSV 读取为 `Dictionary` 列表。
    /// </summary>
    public static List<Dictionary<string, string>> ReadCsv(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = new List<Dictionary<string, string>>();
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            var record = new Dictionary<string, string>();
            foreach (var header in csv.HeaderRecord!)
            {
                record[header] = csv.GetField(header) ?? string.Empty;
            }

            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// 以迭代器形式读取 CSV，每行转为 `Dictionary`。
    /// </summary>
    public static IEnumerable<Dictionary<string, string>> ReadCsvIter(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            var record = new Dictionary<string, string>();
            foreach (var header in csv.HeaderRecord!)
            {
                record[header] = csv.GetField(header) ?? string.Empty;
            }

            yield return record;
        }
    }

    /// <summary>
    /// 写入 CSV 文件，根据首行字段推导表头。
    /// </summary>
    public static void WriteCsv(string path, IEnumerable<Dictionary<string, string>> data)
    {
        var dataList = data.ToList();
        if (dataList.Count == 0) return;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var writer = new StreamWriter(path);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var headers = dataList[0].Keys.ToList();
        foreach (var header in headers)
        {
            csv.WriteField(header);
        }

        csv.NextRecord();

        foreach (var row in dataList)
        {
            foreach (var header in headers)
            {
                csv.WriteField(row.ContainsKey(header) ? row[header] : string.Empty);
            }

            csv.NextRecord();
        }
    }

    /// <summary>
    /// 判断文件是否存在。
    /// </summary>
    public static bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// 判断目录是否存在。
    /// </summary>
    public static bool DirExists(string path) => Directory.Exists(path);

    /// <summary>
    /// 递归创建目录。
    /// </summary>
    public static void CreateDir(string path) => Directory.CreateDirectory(path);

    /// <summary>
    /// 删除文件，若不存在则忽略。
    /// </summary>
    public static void DeleteFile(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        else Console.WriteLine("File not found");
    }

    /// <summary>
    /// 删除目录及其所有子项。
    /// </summary>
    public static void DeleteDir(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
        else Console.WriteLine("File not found");
    }

    /// <summary>
    /// 复制文件（会自动创建目标目录）。
    /// </summary>
    public static void CopyFile(string src, string dst)
    {
        var dir = Path.GetDirectoryName(dst);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.Copy(src, dst, true);
    }

    /// <summary>
    /// 移动文件（复制后删除源文件）。
    /// </summary>
    public static void MoveFile(string src, string dst)
    {
        CopyFile(src, dst);
        DeleteFile(src);
    }

    /// <summary>
    /// 获取文件大小（字节）。
    /// </summary>
    public static long GetFileSize(string path) => new FileInfo(path).Length;

    /// <summary>
    /// 获取文件基础信息（大小/后缀/所在目录等）。
    /// </summary>
    public static Dictionary<string, string> GetFileInfo(string path)
    {
        var info = new FileInfo(path);
        return new Dictionary<string, string>
        {
            { "size", info.Length.ToString() },
            { "is_file", (!info.Attributes.HasFlag(FileAttributes.Directory)).ToString() },
            { "is_dir", info.Attributes.HasFlag(FileAttributes.Directory).ToString() },
            { "extension", info.Extension.TrimStart('.') },
            { "basename", info.Name },
            { "dirname", info.DirectoryName ?? string.Empty }
        };
    }

    /// <summary>
    /// 列举目录下的文件，可按子串过滤。
    /// </summary>
    public static List<string> ListFiles(string path, string? pattern = null)
    {
        if (!Directory.Exists(path)) return new List<string>();
        var files = Directory.GetFiles(path);
        if (pattern != null)
        {
            return files.Where(f => Path.GetFileName(f).Contains(pattern)).ToList();
        }

        return files.ToList();
    }

    /// <summary>
    /// 列举目录下的子目录。
    /// </summary>
    public static List<string> ListDirs(string path)
    {
        if (!Directory.Exists(path)) return new List<string>();
        return Directory.GetDirectories(path).ToList();
    }

    /// <summary>
    /// 将多个文件压缩为 zip。
    /// </summary>
    public static void ZipFiles(IEnumerable<string> files, string zipPath)
    {
        var dir = Path.GetDirectoryName(zipPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                zip.CreateEntryFromFile(file, Path.GetFileName(file));
            }
        }
    }

    /// <summary>
    /// 解压 zip 文件到指定目录。
    /// </summary>
    public static void UnzipFile(string zipPath, string extractPath)
    {
        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
        }

        ZipFile.ExtractToDirectory(zipPath, extractPath, true);
    }

    /// <summary>
    /// 获取文件后缀（不含点）。
    /// </summary>
    public static string? GetFileExtension(string path)
    {
        var ext = Path.GetExtension(path);
        return string.IsNullOrEmpty(ext) ? null : ext.TrimStart('.');
    }

    /// <summary>
    /// 获取文件名，可选是否包含后缀。
    /// </summary>
    public static string? GetFileName(string path, bool withExtension) =>
        withExtension ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);

    /// <summary>
    /// 按顺序拼接路径片段。
    /// </summary>
    public static string JoinPath(params string[] parts) => Path.Combine(parts);

    /// <summary>
    /// 标准化路径，确保跨平台兼容性
    /// </summary>
    /// <param name="path">要标准化的路径</param>
    /// <returns>标准化后的路径</returns>
    /// <example>
    /// <code>
    /// string normalizedPath = NormalizePath(@"C:\Users\Documents\file.txt");
    /// // 在 Windows 上返回: "C:/Users/Documents/file.txt"
    /// // 在 Linux 上返回: "/home/user/documents/file.txt"
    /// </code>
    /// </example>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        
        // 统一使用正斜杠，确保跨平台兼容
        return Path.GetFullPath(path).Replace("\\", "/");
    }

    /// <summary>
    /// 检测单个文件编码 (Simple BOM check).
    /// </summary>
    public static string DetectFileEncoding(string path)
    {
        // Simple BOM check
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var bom = new byte[4];
        var bytesRead = fs.Read(bom, 0, 4);
        if (bytesRead < 2) return "UTF-8 (No BOM)"; // Not enough bytes for BOM detection

        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return "UTF-8";
        if (bom[0] == 0xff && bom[1] == 0xfe) return "UTF-16LE";
        if (bom[0] == 0xfe && bom[1] == 0xff) return "UTF-16BE";
        if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0x00 && bom[3] == 0x00) return "UTF-32LE";
        if (bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xfe && bom[3] == 0xff) return "UTF-32BE";

        return "UTF-8 (No BOM)"; // Default assumption
    }

    /// <summary>
    /// 检测文件或目录中所有文件的编码（移植自 Rust encoding_detector.rs）。
    /// </summary>
    /// <param name="targetPath">文件或目录路径</param>
    /// <returns>如果路径不存在则抛出异常</returns>
    public static void DetectEncoding(string targetPath)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(targetPath));
        }

        var path = Path.GetFullPath(targetPath);
        
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            throw new FileNotFoundException($"Path '{path}' does not exist");
        }

        if (File.Exists(path))
        {
            ReportFile(path);
            return;
        }

        if (Directory.Exists(path))
        {
            Console.WriteLine("TYPE\tENCODING\tPATH");
            var hasFiles = false;
            
            foreach (var filePath in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                hasFiles = true;
                try
                {
                    ReportFile(filePath);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to read {filePath}: {ex.Message}");
                }
            }
            
            if (!hasFiles)
            {
                Console.WriteLine($"(no files found under {path})");
            }
            return;
        }

        throw new ArgumentException($"Path '{path}' is neither file nor directory");
    }

    /// <summary>
    /// 报告单个文件的编码信息。
    /// </summary>
    private static void ReportFile(string filePath)
    {
        var encoding = DetectFileEncodingAdvanced(filePath);
        var canonicalPath = Path.GetFullPath(filePath);
        Console.WriteLine($"FILE\t{encoding}\t{canonicalPath}");
    }

    /// <summary>
    /// 检测单个文件的编码（增强版，支持更多编码类型）。
    /// 移植自 Rust encoding_detector.rs 的 detect_file_encoding 函数。
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>编码名称</returns>
    public static string DetectFileEncodingAdvanced(string path)
    {
        const int bufferSize = 8192;
        
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        // 首先检查 BOM
        var bom = new byte[4];
        var bomBytesRead = fs.Read(bom, 0, 4);
        
        if (bomBytesRead >= 2)
        {
            // UTF-8 BOM
            if (bomBytesRead >= 3 && bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
            {
                return "UTF-8";
            }
            
            // UTF-16LE BOM
            if (bom[0] == 0xff && bom[1] == 0xfe)
            {
                // 检查是否是 UTF-32LE
                if (bomBytesRead >= 4 && bom[2] == 0x00 && bom[3] == 0x00)
                {
                    return "UTF-32LE";
                }
                return "UTF-16LE";
            }
            
            // UTF-16BE BOM
            if (bom[0] == 0xfe && bom[1] == 0xff)
            {
                return "UTF-16BE";
            }
            
            // UTF-32BE BOM
            if (bomBytesRead >= 4 && bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xfe && bom[3] == 0xff)
            {
                return "UTF-32BE";
            }
        }

        // 重置流位置（跳过已读取的 BOM）
        fs.Position = 0;
        
        // 读取文件内容进行启发式检测
        var buffer = new byte[bufferSize];
        var totalBytesRead = 0;
        var nullBytesCount = 0;
        var highBytesCount = 0;
        var asciiBytesCount = 0;
        
        while (totalBytesRead < bufferSize)
        {
            var bytesRead = fs.Read(buffer, totalBytesRead, bufferSize - totalBytesRead);
            if (bytesRead == 0) break;
            totalBytesRead += bytesRead;
        }
        
        // 分析字节模式
        for (int i = 0; i < totalBytesRead; i++)
        {
            var b = buffer[i];
            
            if (b == 0x00)
            {
                nullBytesCount++;
            }
            else if (b < 0x80)
            {
                asciiBytesCount++;
            }
            else if (b >= 0x80)
            {
                highBytesCount++;
            }
        }
        
        // 启发式检测逻辑
        if (nullBytesCount > 0 && totalBytesRead > 0)
        {
            var nullRatio = (double)nullBytesCount / totalBytesRead;
            // 如果包含大量 null 字节，可能是 UTF-16 或 UTF-32
            if (nullRatio > 0.1)
            {
                // 检查是否是 UTF-16LE 模式（每两个字节一个 null）
                var utf16LePattern = true;
                for (int i = 0; i < Math.Min(totalBytesRead - 1, 100); i += 2)
                {
                    if (i + 1 < totalBytesRead && buffer[i + 1] != 0x00 && buffer[i] != 0x00)
                    {
                        utf16LePattern = false;
                        break;
                    }
                }
                if (utf16LePattern) return "UTF-16LE";
                
                // 检查是否是 UTF-16BE 模式
                var utf16BePattern = true;
                for (int i = 0; i < Math.Min(totalBytesRead - 1, 100); i += 2)
                {
                    if (i + 1 < totalBytesRead && buffer[i] != 0x00 && buffer[i + 1] != 0x00)
                    {
                        utf16BePattern = false;
                        break;
                    }
                }
                if (utf16BePattern) return "UTF-16BE";
            }
        }
        
        // 检查 GBK/GB2312 模式（中文字符）
        if (highBytesCount > 0 && totalBytesRead > 0)
        {
            var highByteRatio = (double)highBytesCount / totalBytesRead;
            if (highByteRatio > 0.1)
            {
                // 检查是否是 GBK/GB2312 模式
                var gbkPattern = true;
                for (int i = 0; i < Math.Min(totalBytesRead - 1, 200); i++)
                {
                    if (buffer[i] >= 0x81 && buffer[i] <= 0xFE)
                    {
                        if (i + 1 < totalBytesRead && 
                            buffer[i + 1] >= 0x40 && buffer[i + 1] <= 0xFE && buffer[i + 1] != 0x7F)
                        {
                            // 可能是 GBK
                            return "GBK";
                        }
                    }
                }
            }
        }
        
        // 尝试使用 UTF-8 解码验证
        try
        {
            fs.Position = 0;
            using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: bufferSize, leaveOpen: true);
            var sample = new char[bufferSize];
            var charsRead = reader.Read(sample, 0, bufferSize);
            
            // 检查是否包含无效字符
            var hasInvalidChars = false;
            for (int i = 0; i < charsRead; i++)
            {
                if (char.IsSurrogate(sample[i]) && !char.IsHighSurrogate(sample[i]))
                {
                    hasInvalidChars = true;
                    break;
                }
            }
            
            if (!hasInvalidChars && charsRead > 0)
            {
                return "UTF-8";
            }
        }
        catch
        {
            // UTF-8 解码失败，继续其他检测
        }
        
        // 默认返回
        if (asciiBytesCount == totalBytesRead)
        {
            return "ASCII";
        }
        
        return "UTF-8 (No BOM)";
    }
}