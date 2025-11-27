using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;

namespace UnityBridge.Tools;

/// <summary>
/// 导出数据类型枚举。
/// </summary>
public enum ExportDataType
{
    /// <summary>
    /// 二维字符串列表。
    /// </summary>
    Table,
    /// <summary>
    /// JSON 对象列表。
    /// </summary>
    Json
}

/// <summary>
/// 数据导出工具，支持 Excel (xlsx) 和 CSV 格式。
/// </summary>
public class ExportHelper
{
    private readonly Dictionary<string, object> _sheets = new();
    private readonly Dictionary<string, ExportDataType> _sheetTypes = new();

    /// <summary>
    /// 添加表格数据工作表。
    /// </summary>
    /// <param name="name">工作表名称</param>
    /// <param name="data">二维字符串列表数据</param>
    public void AddSheet(string name, List<List<string>> data)
    {
        _sheets[name] = data;
        _sheetTypes[name] = ExportDataType.Table;
    }

    /// <summary>
    /// 添加 JSON 数据工作表。
    /// </summary>
    /// <param name="name">工作表名称</param>
    /// <param name="data">字典列表数据</param>
    public void AddSheet(string name, List<Dictionary<string, object>> data)
    {
        _sheets[name] = data;
        _sheetTypes[name] = ExportDataType.Json;
    }

    /// <summary>
    /// 添加表格数据（别名）。
    /// </summary>
    public void AddTableData(string name, List<List<string>> data)
    {
        AddSheet(name, data);
    }

    /// <summary>
    /// 添加对象列表数据（自动序列化为 JSON）。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="name">工作表名称</param>
    /// <param name="data">对象列表</param>
    public void AddJsonData<T>(string name, IEnumerable<T> data)
    {
        // Convert to List<Dictionary<string, object>>
        var list = new List<Dictionary<string, object>>();
        foreach (var item in data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(item);
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (dict != null)
            {
                list.Add(dict);
            }
        }
        AddSheet(name, list);
    }
    
    /// <summary>
    /// 添加对象列表数据，并指定导出的列。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="name">工作表名称</param>
    /// <param name="data">对象列表</param>
    /// <param name="columns">需要导出的列名列表</param>
    public void AddJsonDataWithColumns<T>(string name, IEnumerable<T> data, List<string> columns)
    {
        var table = new List<List<string>>();
        table.Add(columns); // Header

        foreach (var item in data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(item);
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            
            var row = new List<string>();
            foreach (var col in columns)
            {
                if (dict != null && dict.TryGetValue(col, out var val))
                {
                    row.Add(val.ToString());
                }
                else
                {
                    row.Add("");
                }
            }
            table.Add(row);
        }
        AddSheet(name, table);
    }

    /// <summary>
    /// 导出所有工作表到 Excel 文件。
    /// </summary>
    /// <param name="path">输出文件路径 (.xlsx)</param>
    public void ExportToExcel(string path)
    {
        using var workbook = new XLWorkbook();
        foreach (var sheetName in _sheets.Keys)
        {
            var type = _sheetTypes[sheetName];
            var data = _sheets[sheetName];
            var worksheet = workbook.Worksheets.Add(sheetName);

            if (type == ExportDataType.Table)
            {
                var tableData = (List<List<string>>)data;
                for (int r = 0; r < tableData.Count; r++)
                {
                    for (int c = 0; c < tableData[r].Count; c++)
                    {
                        worksheet.Cell(r + 1, c + 1).Value = tableData[r][c];
                    }
                }
            }
            else if (type == ExportDataType.Json)
            {
                var jsonData = (List<Dictionary<string, object>>)data;
                if (jsonData.Count > 0)
                {
                    // Headers
                    var headers = jsonData.SelectMany(d => d.Keys).Distinct().ToList();
                    for (int c = 0; c < headers.Count; c++)
                    {
                        worksheet.Cell(1, c + 1).Value = headers[c];
                    }

                    // Data
                    for (int r = 0; r < jsonData.Count; r++)
                    {
                        for (int c = 0; c < headers.Count; c++)
                        {
                            if (jsonData[r].TryGetValue(headers[c], out var val))
                            {
                                worksheet.Cell(r + 2, c + 1).Value = val?.ToString() ?? "";
                            }
                        }
                    }
                }
            }
        }
        workbook.SaveAs(path);
    }

    /// <summary>
    /// 导出指定工作表到 CSV 文件。
    /// </summary>
    /// <param name="path">输出文件路径 (.csv)</param>
    /// <param name="sheetName">工作表名称（若只有一个工作表可省略）</param>
    /// <exception cref="ArgumentException">未找到工作表或存在多个工作表但未指定名称时抛出</exception>
    public void ExportToCsv(string path, string? sheetName = null)
    {
        object data;
        ExportDataType type;

        if (sheetName != null)
        {
            if (!_sheets.ContainsKey(sheetName)) throw new ArgumentException($"Sheet {sheetName} not found");
            data = _sheets[sheetName];
            type = _sheetTypes[sheetName];
        }
        else if (_sheets.Count == 1)
        {
            var first = _sheets.Keys.First();
            data = _sheets[first];
            type = _sheetTypes[first];
        }
        else
        {
            throw new ArgumentException("Multiple sheets found. Please specify sheet name.");
        }

        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        // Write BOM
        writer.BaseStream.Write(Encoding.UTF8.GetPreamble(), 0, Encoding.UTF8.GetPreamble().Length);
        
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        if (type == ExportDataType.Table)
        {
            var tableData = (List<List<string>>)data;
            foreach (var row in tableData)
            {
                foreach (var field in row)
                {
                    csv.WriteField(field);
                }
                csv.NextRecord();
            }
        }
        else if (type == ExportDataType.Json)
        {
            var jsonData = (List<Dictionary<string, object>>)data;
            if (jsonData.Count > 0)
            {
                var headers = jsonData.SelectMany(d => d.Keys).Distinct().ToList();
                foreach (var header in headers)
                {
                    csv.WriteField(header);
                }
                csv.NextRecord();

                foreach (var row in jsonData)
                {
                    foreach (var header in headers)
                    {
                        csv.WriteField(row.ContainsKey(header) ? row[header]?.ToString() : "");
                    }
                    csv.NextRecord();
                }
            }
        }
    }
}
