using System.Text.RegularExpressions;

namespace UnityBridge.Helpers;

public static class HeaderHelper
{
    public static Dictionary<string, string> ParseHeaders(string curlString)
    {
        var headers = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(curlString))
            return headers;

        var lines = curlString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            if (trimmedLine.EndsWith(","))
                trimmedLine = trimmedLine[..^1];

            var parts = trimmedLine.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
                continue;

            var name = parts[0].Trim().Trim('\'', '"', ' ');
            var value = parts[1].Trim().Trim('\'', '"', ' ');

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                headers[name] = value;
        }

        return headers;
    }
}