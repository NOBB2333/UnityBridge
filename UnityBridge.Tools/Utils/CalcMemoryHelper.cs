using System;

namespace UnityBridge.Tools;

/// <summary>
/// 与原 `CalcMemoryHelper.py` 行为一致的内存单位换算工具。
/// </summary>
public static class CalcMemoryHelper
{
    /// <summary>
    /// 将字节数格式化为易读的字符串。
    /// </summary>
    public static string FormatBytes(double bytesValue)
    {
        var (scaled, unit) = Scale(Math.Abs(bytesValue));
        return $"{scaled:F2}{unit}";
    }

    /// <summary>
    /// 针对整数输入的便捷封装。
    /// </summary>
    public static string FormatBytes(long bytesValue) => FormatBytes((double)bytesValue);

    /// <summary>
    /// 计算缩放后的数值及显示单位。
    /// </summary>
    private static (double, string) Scale(double value)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;
        const double TB = GB * 1024.0;

        var thresholds = new[] { (TB, "TB"), (GB, "GB"), (MB, "MB"), (KB, "KB") };

        foreach (var (threshold, unit) in thresholds)
        {
            if (value >= threshold)
                return (value / threshold, unit);
        }
        return (value, "B");
    }
}
