using UnityBridge.Tools;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// CalcMemoryHelper 使用示例
/// </summary>
public static class CalcMemoryHelperExamples
{
    public static void Run()
    {
        // 示例 1: 格式化字节数
        Console.WriteLine("1. 格式化字节数:");
        var sizes = new[] { 1024L, 1024 * 1024L, 1024 * 1024 * 1024L, 1024L * 1024 * 1024 * 1024 };
        foreach (var size in sizes)
        {
            var formatted = CalcMemoryHelper.FormatBytes(size);
            Console.WriteLine($"   {size} 字节 = {formatted}");
        }

        // 示例 2: 不同大小的格式化
        Console.WriteLine("\n2. 不同大小的格式化:");
        Console.WriteLine($"   500 字节 = {CalcMemoryHelper.FormatBytes(500)}");
        Console.WriteLine($"   1.5 MB = {CalcMemoryHelper.FormatBytes(1.5 * 1024 * 1024)}");
        Console.WriteLine($"   2.5 GB = {CalcMemoryHelper.FormatBytes(2.5 * 1024 * 1024 * 1024)}");
    }
}

