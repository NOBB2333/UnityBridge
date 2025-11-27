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
        fs.Read(bom, 0, 4);

        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return "UTF-8";
        if (bom[0] == 0xff && bom[1] == 0xfe) return "UTF-16LE";
        if (bom[0] == 0xfe && bom[1] == 0xff) return "UTF-16BE";
        if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0x00 && bom[3] == 0x00) return "UTF-32LE";
        if (bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xfe && bom[3] == 0xff) return "UTF-32BE";

        return "UTF-8 (No BOM)"; // Default assumption
    }
}