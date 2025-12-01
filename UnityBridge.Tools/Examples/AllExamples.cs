namespace UnityBridge.Tools.Examples;

/// <summary>
/// 所有 Helper 示例的汇总执行类
/// </summary>
public static class AllExamples
{
    /// <summary>
    /// 运行所有 Helper 类的示例
    /// </summary>
    public static void RunAll()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("   UnityBridge Helper 示例集合");
        Console.WriteLine("========================================\n");

        Console.WriteLine("=== URLHelper 使用示例 ===\n");
        URLHelperExamples.Run();
        
        Console.WriteLine("\n=== JsonHelper 使用示例 ===\n");
        JsonHelperExamples.Run();
        
        Console.WriteLine("\n=== FileHelper 使用示例 ===\n");
        FileHelperExamples.Run();
        
        Console.WriteLine("\n=== CryptoHelper 使用示例 ===\n");
        CryptoHelperExamples.Run();
        
        Console.WriteLine("\n=== StringHelper 使用示例 ===\n");
        StringHelperExamples.Run();
        
        Console.WriteLine("\n=== DateTimeHelper 使用示例 ===\n");
        DateTimeHelperExamples.Run();
        
        Console.WriteLine("\n=== NetworkHelper 使用示例 ===\n");
        NetworkHelperExamples.Run();
        
        Console.WriteLine("\n=== CalcMemoryHelper 使用示例 ===\n");
        CalcMemoryHelperExamples.Run();
        
        Console.WriteLine("\n=== ExportHelper 使用示例 ===\n");
        ExportHelperExamples.Run();

        Console.WriteLine("\n========================================");
        Console.WriteLine("   所有示例执行完成！");
        Console.WriteLine("========================================");
    }
}

