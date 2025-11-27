namespace UnityBridge.Helpers;

public static class SanitizationHelper
{
    public static string SanitizeFilename(string name)
    {
        var forbidden = new HashSet<char> { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        var chars = name.Select(c => (forbidden.Contains(c) || char.IsControl(c)) ? '_' : c).ToArray();
        var s = new string(chars).Trim().TrimEnd('.');
        return string.IsNullOrEmpty(s) ? "unnamed" : s;
    }
}
