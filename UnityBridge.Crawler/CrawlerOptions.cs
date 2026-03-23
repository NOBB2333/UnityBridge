namespace UnityBridge.Crawler;

/// <summary>
/// 爬虫配置模型。
/// </summary>
public class CrawlerOptions
{
    /// <summary>签名服务地址。</summary>
    public string SignServerUrl { get; set; } = string.Empty;

    /// <summary>数据库配置。</summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>默认延迟配置。</summary>
    public DelayOptions DefaultDelay { get; set; } = new();

    /// <summary>默认最大页数。</summary>
    public int MaxPages { get; set; } = 10;

    /// <summary>各平台配置。</summary>
    public PlatformsOptions Platforms { get; set; } = new();
}

public class DatabaseOptions
{
    /// <summary>
    /// 数据库连接字符串。类型自动从连接串识别，支持 SQLite / MySQL / PostgreSQL / SqlServer / Oracle。
    /// 示例：
    ///   SQLite:     Data Source=crawler.db;
    ///   PostgreSQL: HOST=127.0.0.1;PORT=5432;DATABASE=crawler;USERNAME=postgres;PASSWORD=xxx;
    ///   MySQL:      Server=127.0.0.1;Port=3306;Database=crawler;Uid=root;Pwd=xxx;
    ///   SqlServer:  Server=localhost;Database=crawler;User Id=xxx;Password=xxx;
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}

public class DelayOptions
{
    /// <summary>最小延迟（毫秒）。</summary>
    public int MinMs { get; set; } = 1000;

    /// <summary>最大延迟（毫秒）。</summary>
    public int MaxMs { get; set; } = 3000;
}

public class PlatformsOptions
{
    public PlatformConfig XiaoHongShu { get; set; } = new();
    public PlatformConfig BiliBili { get; set; } = new();
    public PlatformConfig Douyin { get; set; } = new();
    public PlatformConfig Tieba { get; set; } = new();
    public PlatformConfig Kuaishou { get; set; } = new();
    public PlatformConfig Zhihu { get; set; } = new();
    public PlatformConfig Weibo { get; set; } = new();
}

public class PlatformConfig
{
    /// <summary>是否启用。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>平台 Cookies。</summary>
    public string Cookies { get; set; } = string.Empty;
}
