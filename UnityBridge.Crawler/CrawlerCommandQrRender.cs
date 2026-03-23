using QRCoder;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace UnityBridge.Crawler;

internal static class CrawlerCommandQrRender
{
    internal static async Task RunLoginCheckAsync(string platformName, Func<Task<bool>> action)
    {
        try
        {
            var ok = await action();
            Console.WriteLine($"[Login] {platformName}: {(ok ? "OK" : "FAIL")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Login] {platformName}: FAIL ({ex.Message})");
        }
    }

    internal static void RenderQrCodeToConsole(string qrContent)
    {
        var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
        var matrix = BuildQrMatrix(data);
        if (matrix.Count == 0)
        {
            return;
        }

        var consoleWidth = GetConsoleWidthOrDefault();
        var consoleHeight = GetConsoleHeightOrDefault();
        var requiredWidth = matrix.Count;
        var requiredHeight = (matrix.Count + 1) / 2;

        if (requiredWidth > consoleWidth || requiredHeight > consoleHeight)
        {
            Console.WriteLine($"[Login] 当前终端可能过小，建议至少 {requiredWidth}x{requiredHeight}（宽x高）字符窗口。");
        }

        RenderCompactQr(matrix);
    }

    internal static bool TryUpdateCookiesInConfig(
        string platformKey,
        string cookies,
        out string configPath,
        out string error)
    {
        configPath = Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "appsettings.json");
        error = string.Empty;

        try
        {
            if (!File.Exists(configPath))
            {
                error = $"找不到配置文件：{configPath}";
                return false;
            }

            var root = JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject;
            var platforms = root?["Crawler"]?["Platforms"] as JsonObject;
            var platform = platforms?[platformKey] as JsonObject;
            if (platform is null)
            {
                error = $"配置中不存在平台节点：Crawler.Platforms.{platformKey}";
                return false;
            }

            platform["Cookies"] = cookies;
            var json = root!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static List<List<bool>> BuildQrMatrix(QRCodeData data)
    {
        var raw = new List<List<bool>>(data.ModuleMatrix.Count);
        for (var y = 0; y < data.ModuleMatrix.Count; y++)
        {
            var row = new List<bool>(data.ModuleMatrix[y].Count);
            for (var x = 0; x < data.ModuleMatrix[y].Count; x++)
            {
                row.Add(data.ModuleMatrix[y][x] == true);
            }
            raw.Add(row);
        }

        return TrimQuietZone(raw, padding: 1);
    }

    private static List<List<bool>> TrimQuietZone(List<List<bool>> matrix, int padding)
    {
        var size = matrix.Count;
        var minY = size;
        var maxY = -1;
        var minX = size;
        var maxX = -1;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < matrix[y].Count; x++)
            {
                if (!matrix[y][x])
                {
                    continue;
                }

                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }

        if (maxY < 0 || maxX < 0)
        {
            return matrix;
        }

        minY = Math.Max(0, minY - padding);
        minX = Math.Max(0, minX - padding);
        maxY = Math.Min(size - 1, maxY + padding);
        maxX = Math.Min(size - 1, maxX + padding);

        var result = new List<List<bool>>(maxY - minY + 1);
        for (var y = minY; y <= maxY; y++)
        {
            var row = new List<bool>(maxX - minX + 1);
            for (var x = minX; x <= maxX; x++)
            {
                row.Add(matrix[y][x]);
            }
            result.Add(row);
        }

        return result;
    }

    private static void RenderCompactQr(List<List<bool>> matrix)
    {
        for (var y = 0; y < matrix.Count; y += 2)
        {
            var sb = new StringBuilder(matrix.Count);
            var hasNext = y + 1 < matrix.Count;

            for (var x = 0; x < matrix[y].Count; x++)
            {
                var top = matrix[y][x];
                var bottom = hasNext && matrix[y + 1][x];
                sb.Append((top, bottom) switch
                {
                    (true, true) => '█',
                    (true, false) => '▀',
                    (false, true) => '▄',
                    _ => ' '
                });
            }

            Console.WriteLine(sb.ToString());
        }
    }

    private static int GetConsoleWidthOrDefault(int fallback = 120)
    {
        try
        {
            return Console.WindowWidth > 0 ? Console.WindowWidth : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static int GetConsoleHeightOrDefault(int fallback = 40)
    {
        try
        {
            return Console.WindowHeight > 0 ? Console.WindowHeight : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
