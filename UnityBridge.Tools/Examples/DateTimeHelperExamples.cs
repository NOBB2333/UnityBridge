using UnityBridge.Tools;

namespace UnityBridge.Tools.Examples;

/// <summary>
/// DateTimeHelper 使用示例
/// </summary>
public static class DateTimeHelperExamples
{
    public static void Run()
    {
        // 示例 1: 获取当前时间
        Console.WriteLine("1. 获取当前时间:");
        var currentTime = DateTimeHelper.GetCurrentTime("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"   当前时间: {currentTime}");
        var timestamp = DateTimeHelper.GetCurrentTimestamp();
        Console.WriteLine($"   当前时间戳: {timestamp}");

        // 示例 2: 时间戳转换
        Console.WriteLine("\n2. 时间戳转换:");
        var ts = 1704067200L; // 2024-01-01 00:00:00
        var datetime = DateTimeHelper.TimestampToDatetime(ts, "yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"   时间戳转日期: {datetime}");
        var backToTs = DateTimeHelper.DatetimeToTimestamp(datetime, "yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"   日期转时间戳: {backToTs}");

        // 示例 3: 日期格式转换
        Console.WriteLine("\n3. 日期格式转换:");
        var date1 = "2024-01-01";
        var date2 = DateTimeHelper.FormatDatetime(date1, "yyyy-MM-dd", "yyyy年MM月dd日");
        Console.WriteLine($"   格式转换: {date1} -> {date2}");

        // 示例 4: 日期计算
        Console.WriteLine("\n4. 日期计算:");
        var baseDate = "2024-01-01";
        var format = "yyyy-MM-dd";
        Console.WriteLine($"   加 7 天: {DateTimeHelper.AddDays(baseDate, format, 7)}");
        Console.WriteLine($"   加 2 小时: {DateTimeHelper.AddHours(baseDate + " 12:00:00", format + " HH:mm:ss", 2)}");

        // 示例 5: 日期范围
        Console.WriteLine("\n5. 日期范围:");
        var dateRange = DateTimeHelper.GetDateRange("2024-01-01", "2024-01-05", "yyyy-MM-dd");
        Console.WriteLine($"   日期范围 ({dateRange.Count} 天):");
        foreach (var date in dateRange)
        {
            Console.WriteLine($"   - {date}");
        }

        // 示例 6: 星期几
        Console.WriteLine("\n6. 星期几:");
        var weekday = DateTimeHelper.GetWeekday("2024-01-01", "yyyy-MM-dd");
        var weekdayName = DateTimeHelper.GetWeekdayName("2024-01-01", "yyyy-MM-dd");
        Console.WriteLine($"   2024-01-01 是: {weekdayName} (索引: {weekday})");
        Console.WriteLine($"   是否周末: {DateTimeHelper.IsWeekend("2024-01-01", "yyyy-MM-dd")}");

        // 示例 7: 月份信息
        Console.WriteLine("\n7. 月份信息:");
        var monthDays = DateTimeHelper.GetMonthDays(2024, 2);
        Console.WriteLine($"   2024年2月有 {monthDays} 天");
        var (first, last) = DateTimeHelper.GetMonthFirstLastDay(2024, 2);
        Console.WriteLine($"   第一天: {first}, 最后一天: {last}");

        // 示例 8: 年龄计算
        Console.WriteLine("\n8. 年龄计算:");
        var age = DateTimeHelper.CalculateAge("1990-01-01", "yyyy-MM-dd");
        Console.WriteLine($"   1990-01-01 出生，当前年龄: {age} 岁");

        // 示例 9: 时间差
        Console.WriteLine("\n9. 时间差:");
        var timeDiff = DateTimeHelper.TimeDiff("2024-01-01 10:00:00", "2024-01-01 12:30:00", "yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"   时间差: {timeDiff.TotalHours:F2} 小时");
    }
}

