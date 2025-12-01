using UnityBridge.Tools;

namespace UnityBridge.Commands;

/// <summary>
/// 文件/文件夹编码检测命令（移植自 Rust encoding_detector.rs）
/// </summary>
public static class DetectEncodingCommand
{
    public static async Task RunAsync()
    {
        while (true)
        {
            Console.WriteLine("\n文件/文件夹编码检测");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.Write("请输入文件或文件夹路径（输入 'q' 返回主菜单）: ");
            
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input) || input.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            
            try
            {
                FileHelper.DetectEncoding(input);
                Console.WriteLine("\n检测完成。按任意键继续...");
                Console.ReadKey();
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"\n错误: {ex.Message}");
                Console.WriteLine("按任意键继续...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\n发生错误: {ex.Message}");
                Console.WriteLine("按任意键继续...");
                Console.ReadKey();
            }
        }
    }
}

