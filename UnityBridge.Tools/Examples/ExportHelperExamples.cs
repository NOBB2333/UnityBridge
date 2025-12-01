using UnityBridge.Tools;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// ExportHelper 使用示例
/// </summary>
public static class ExportHelperExamples
{
    public static void Run()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "UnityBridgeExamples");
        Directory.CreateDirectory(testDir);

        try
        {
            // 示例 1: 导出表格数据到 Excel
            Console.WriteLine("1. 导出表格数据到 Excel:");
            var exportHelper1 = new ExportHelper();
            var tableData = new List<List<string>>
            {
                new() { "姓名", "年龄", "城市" },
                new() { "张三", "25", "北京" },
                new() { "李四", "30", "上海" }
            };
            exportHelper1.AddSheet("用户信息", tableData);
            var excelPath = Path.Combine(testDir, "users.xlsx");
            exportHelper1.ExportToExcel(excelPath);
            Console.WriteLine($"   已导出到: {excelPath}");

            // 示例 2: 导出 JSON 数据到 Excel
            Console.WriteLine("\n2. 导出 JSON 数据到 Excel:");
            var exportHelper2 = new ExportHelper();
            var jsonData = new List<Dictionary<string, object>>
            {
                new() { ["name"] = "王五", ["age"] = 28, ["city"] = "广州" },
                new() { ["name"] = "赵六", ["age"] = 35, ["city"] = "深圳" }
            };
            exportHelper2.AddSheet("员工信息", jsonData);
            var excelPath2 = Path.Combine(testDir, "employees.xlsx");
            exportHelper2.ExportToExcel(excelPath2);
            Console.WriteLine($"   已导出到: {excelPath2}");

            // 示例 3: 导出到 CSV
            Console.WriteLine("\n3. 导出到 CSV:");
            var exportHelper3 = new ExportHelper();
            exportHelper3.AddSheet("数据", tableData);
            var csvPath = Path.Combine(testDir, "data.csv");
            exportHelper3.ExportToCsv(csvPath);
            Console.WriteLine($"   已导出到: {csvPath}");

            // 示例 4: 多工作表导出
            Console.WriteLine("\n4. 多工作表导出:");
            var exportHelper4 = new ExportHelper();
            exportHelper4.AddSheet("工作表1", tableData);
            exportHelper4.AddSheet("工作表2", jsonData);
            var multiSheetPath = Path.Combine(testDir, "multi_sheet.xlsx");
            exportHelper4.ExportToExcel(multiSheetPath);
            Console.WriteLine($"   已导出多工作表到: {multiSheetPath}");

            // 示例 5: 使用对象列表导出
            Console.WriteLine("\n5. 使用对象列表导出:");
            var exportHelper5 = new ExportHelper();
            var users = new[]
            {
                new { Name = "张三", Age = 25, City = "北京" },
                new { Name = "李四", Age = 30, City = "上海" }
            };
            exportHelper5.AddJsonData("用户", users);
            var objPath = Path.Combine(testDir, "objects.xlsx");
            exportHelper5.ExportToExcel(objPath);
            Console.WriteLine($"   已导出对象列表到: {objPath}");

            Console.WriteLine("\n   注意: 示例文件已创建在临时目录，可以手动查看");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   错误: {ex.Message}");
        }
    }
}

