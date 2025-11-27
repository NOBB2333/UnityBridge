using System.Globalization;

namespace UnityBridge.Tools;

/// <summary>
/// 日期时间处理工具类。
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// 获取当前时间字符串。
    /// </summary>
    /// <param name="format">日期格式，如 "yyyy-MM-dd HH:mm:ss"</param>
    /// <returns>格式化后的当前时间字符串</returns>
    public static string GetCurrentTime(string format) => DateTime.Now.ToString(format);

    /// <summary>
    /// 获取当前时间戳（秒）。
    /// </summary>
    /// <returns>Unix 时间戳（秒）</returns>
    public static long GetCurrentTimestamp() => new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

    /// <summary>
    /// 将时间戳转换为日期时间字符串。
    /// </summary>
    /// <param name="timestamp">Unix 时间戳（秒）</param>
    /// <param name="format">目标格式，默认为 ISO 8601 格式</param>
    /// <returns>格式化后的日期时间字符串</returns>
    public static string TimestampToDatetime(long timestamp, string? format = null)
    {
        var dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime();
        return format != null ? dt.ToString(format) : dt.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }

    /// <summary>
    /// 将日期时间字符串转换为时间戳。
    /// </summary>
    /// <param name="datetime">日期时间字符串</param>
    /// <param name="format">日期格式</param>
    /// <returns>Unix 时间戳（秒）</returns>
    /// <exception cref="ArgumentException">解析失败时抛出</exception>
    public static long DatetimeToTimestamp(string datetime, string format)
    {
        if (DateTime.TryParseExact(datetime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return new DateTimeOffset(dt).ToUnixTimeSeconds();

        throw new ArgumentException("Failed to parse datetime", nameof(datetime));
    }

    /// <summary>
    /// 转换日期时间字符串的格式。
    /// </summary>
    /// <param name="datetime">原日期时间字符串</param>
    /// <param name="fromFormat">原格式</param>
    /// <param name="toFormat">目标格式</param>
    /// <returns>新格式的日期时间字符串</returns>
    public static string FormatDatetime(string datetime, string fromFormat, string toFormat)
    {
        if (DateTime.TryParseExact(datetime, fromFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.ToString(toFormat);

        throw new ArgumentException("Failed to parse datetime", nameof(datetime));
    }

    /// <summary>
    /// 日期加天数。
    /// </summary>
    /// <param name="datetime">基准日期</param>
    /// <param name="format">日期格式</param>
    /// <param name="days">增加的天数（可为负数）</param>
    /// <returns>计算后的日期字符串</returns>
    public static string AddDays(string datetime, string format, int days)
    {
        if (DateTime.TryParseExact(datetime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.AddDays(days).ToString(format);

        throw new ArgumentException("Failed to parse datetime", nameof(datetime));
    }

    /// <summary>
    /// 日期加小时。
    /// </summary>
    /// <param name="datetime">基准日期</param>
    /// <param name="format">日期格式</param>
    /// <param name="hours">增加的小时数</param>
    /// <returns>计算后的日期字符串</returns>
    public static string AddHours(string datetime, string format, int hours)
    {
        if (DateTime.TryParseExact(datetime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.AddHours(hours).ToString(format);

        throw new ArgumentException("Failed to parse datetime", nameof(datetime));
    }

    /// <summary>
    /// 日期加分钟。
    /// </summary>
    /// <param name="datetime">基准日期</param>
    /// <param name="format">日期格式</param>
    /// <param name="minutes">增加的分钟数</param>
    /// <returns>计算后的日期字符串</returns>
    public static string AddMinutes(string datetime, string format, int minutes)
    {
        if (DateTime.TryParseExact(datetime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.AddMinutes(minutes).ToString(format);

        throw new ArgumentException("Failed to parse datetime", nameof(datetime));
    }

    /// <summary>
    /// 获取两个日期之间的所有日期列表（包含开始和结束）。
    /// </summary>
    /// <param name="start">开始日期</param>
    /// <param name="end">结束日期</param>
    /// <param name="format">日期格式</param>
    /// <returns>日期字符串列表</returns>
    public static List<string> GetDateRange(string start, string end, string format)
    {
        if (!DateTime.TryParseExact(start, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var startDate) ||
            !DateTime.TryParseExact(end, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
        {
            throw new ArgumentException("Failed to parse start or end date");
        }

        var dates = new List<string>();
        for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            dates.Add(dt.ToString(format));

        return dates;
    }

    /// <summary>
    /// 获取日期是星期几（0=周一, 6=周日）。
    /// </summary>
    /// <param name="datetime">日期字符串</param>
    /// <param name="format">日期格式</param>
    /// <returns>0-6 的整数</returns>
    public static int GetWeekday(string datetime, string format)
    {
        if (DateTime.TryParseExact(datetime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            // Monday is 0 in Rust implementation (chrono::Weekday::num_days_from_monday)
            // DayOfWeek.Monday is 1 in C#
            // We need to map Sunday (0) -> 6, Monday (1) -> 0, etc.
            var day = dt.DayOfWeek;
            return day == DayOfWeek.Sunday ? 6 : (int)day - 1;
        }

        throw new ArgumentException("Failed to parse date", nameof(datetime));
    }

    /// <summary>
    /// 获取星期几的中文名称（如“周一”）。
    /// </summary>
    /// <param name="datetime">日期字符串</param>
    /// <param name="format">日期格式</param>
    /// <returns>中文星期名称</returns>
    public static string GetWeekdayName(string datetime, string format)
    {
        string[] weekdayNames = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        var weekday = GetWeekday(datetime, format);
        return weekdayNames[weekday];
    }

    /// <summary>
    /// 判断是否为周末（周六或周日）。
    /// </summary>
    /// <param name="datetime">日期字符串</param>
    /// <param name="format">日期格式</param>
    /// <returns>如果是周末返回 true</returns>
    public static bool IsWeekend(string datetime, string format) => GetWeekday(datetime, format) >= 5;

    /// <summary>
    /// 获取某个月份的天数。
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>天数</returns>
    public static int GetMonthDays(int year, int month) => DateTime.DaysInMonth(year, month);

    /// <summary>
    /// 获取某个月份的第一天和最后一天。
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>元组 (第一天, 最后一天)，格式为 yyyy-MM-dd</returns>
    public static (string, string) GetMonthFirstLastDay(int year, int month)
    {
        var first = new DateTime(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        return (first.ToString("yyyy-MM-dd"), last.ToString("yyyy-MM-dd"));
    }

    /// <summary>
    /// 根据出生日期计算年龄。
    /// </summary>
    /// <param name="birthDate">出生日期</param>
    /// <param name="format">日期格式</param>
    /// <param name="currentDate">当前日期（可选，默认为今天）</param>
    /// <returns>周岁年龄</returns>
    public static int CalculateAge(string birthDate, string format, string? currentDate = null)
    {
        if (!DateTime.TryParseExact(birthDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var birth))
        {
            throw new ArgumentException("Invalid birth date", nameof(birthDate));
        }

        DateTime current;
        if (currentDate != null)
        {
            if (!DateTime.TryParseExact(currentDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out current))
            {
                throw new ArgumentException("Invalid current date", nameof(currentDate));
            }
        }
        else
        {
            current = DateTime.Now;
        }

        var age = current.Year - birth.Year;
        if (current.Month < birth.Month || (current.Month == birth.Month && current.Day < birth.Day))
        {
            age--;
        }

        return age;
    }

    /// <summary>
    /// 计算两个时间的时间差。
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="format">时间格式</param>
    /// <returns>TimeSpan 对象</returns>
    public static TimeSpan TimeDiff(string start, string end, string format)
    {
        if (!DateTime.TryParseExact(start, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var startDt) ||
            !DateTime.TryParseExact(end, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDt))
        {
            throw new ArgumentException("Invalid start or end datetime");
        }

        return endDt - startDt;
    }
}