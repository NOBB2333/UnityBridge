using System.Globalization;
using CsvHelper;

namespace UnityBridge.Shared;

/// <summary>
/// CSV 导出辅助类。
/// </summary>
public static class CsvExportHelper
{
    /// <summary>
    /// 将记录导出到 CSV 文件。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="records">要导出的记录集合。</param>
    /// <param name="exportDirectory">导出目录。</param>
    /// <param name="fileName">文件名（不含路径）。</param>
    /// <returns>导出的完整文件路径。</returns>
    public static string Export<T>(IEnumerable<T> records, string exportDirectory, string fileName)
    {
        EnsureDirectoryExists(exportDirectory);
        
        var csvPath = Path.Combine(exportDirectory, fileName);
        using var writer = new StreamWriter(csvPath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(records);
        
        return csvPath;
    }

    /// <summary>
    /// 将记录导出到 CSV 文件，并打印结果。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="records">要导出的记录列表。</param>
    /// <param name="exportDirectory">导出目录。</param>
    /// <param name="fileName">文件名（不含路径）。</param>
    /// <param name="recordDescription">记录描述（用于打印，如"条记录"、"个应用"等）。</param>
    /// <returns>导出的完整文件路径。</returns>
    public static string ExportAndPrint<T>(IList<T> records, string exportDirectory, string fileName, string recordDescription = "条记录")
    {
        var csvPath = Export(records, exportDirectory, fileName);
        Console.WriteLine($"已将 {records.Count} {recordDescription}导出到 {csvPath}");
        return csvPath;
    }

    /// <summary>
    /// 确保目录存在。
    /// </summary>
    /// <param name="directory">目录路径。</param>
    public static void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
