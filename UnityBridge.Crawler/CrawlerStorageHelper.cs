using SqlSugar;
using UnityBridge.Crawler.XiaoHongShu.Models;
using UnityBridge.Crawler.BiliBili.Models;
using UnityBridge.Crawler.Douyin.Models;
using UnityBridge.Crawler.Tieba.Models;
using UnityBridge.Crawler.Kuaishou.Models;
using UnityBridge.Crawler.Zhihu.Models;
using UnityBridge.Crawler.Weibo.Models;

namespace UnityBridge.Crawler;

/// <summary>
/// 爬虫存储辅助类。
/// </summary>
public static class CrawlerStorageHelper
{
    /// <summary>
    /// 根据连接串自动识别数据库类型并创建客户端，由 SqlSugar CodeFirst 自动建表。
    /// 支持 SQLite / MySQL / PostgreSQL / SqlServer / Oracle。
    /// </summary>
    public static SqlSugarClient CreateDb(DatabaseOptions options)
    {
        StaticConfig.EnableAot = true;

        DbType detectDbType(string conn)
        {
            var s = conn.ToLowerInvariant();
            if (s.Contains("oracle") || (s.Contains("data source") && s.Contains("user id") && !s.Contains("sqlite") && !s.Contains(".db"))) return DbType.Oracle;
            if (s.Contains("postgresql") || s.Contains("host=") && s.Contains("username=")) return DbType.PostgreSQL;
            if (s.Contains("mysql") || (s.Contains("server=") && s.Contains("port="))) return DbType.MySql;
            if (s.Contains("sqlite") || s.Contains(".db")) return DbType.Sqlite;
            if (s.Contains("kingbase") || s.Contains("kdb")) return DbType.Kdbndp;
            return DbType.SqlServer;
        }

        var dbType = detectDbType(options.ConnectionString);

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = options.ConnectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        InitAllTables(db);

        Console.WriteLine($"[Storage] {dbType} 数据库已初始化：{options.ConnectionString}");

        return db;
    }

    /// <summary>
    /// 初始化所有平台的表。
    /// </summary>
    private static void InitAllTables(SqlSugarClient db)
    {
        // 小红书
        db.CodeFirst.InitTables<XhsNoteCard>();
        db.CodeFirst.InitTables<XhsComment>();
        db.CodeFirst.InitTables<XhsCreator>();

        // B站
        db.CodeFirst.InitTables<BiliVideo>();
        db.CodeFirst.InitTables<BiliComment>();
        db.CodeFirst.InitTables<BiliCreator>();

        // 抖音
        db.CodeFirst.InitTables<DouyinAweme>();
        db.CodeFirst.InitTables<DouyinComment>();
        db.CodeFirst.InitTables<DouyinCreator>();

        // 百度贴吧
        db.CodeFirst.InitTables<TiebaPost>();
        db.CodeFirst.InitTables<TiebaComment>();
        db.CodeFirst.InitTables<TiebaCreator>();

        // 快手
        db.CodeFirst.InitTables<KuaishouVideo>();
        db.CodeFirst.InitTables<KuaishouComment>();
        db.CodeFirst.InitTables<KuaishouCreator>();

        // 知乎
        db.CodeFirst.InitTables<ZhihuContent>();
        db.CodeFirst.InitTables<ZhihuComment>();
        db.CodeFirst.InitTables<ZhihuCreator>();

        // 微博
        db.CodeFirst.InitTables<WeiboNote>();
        db.CodeFirst.InitTables<WeiboComment>();
        db.CodeFirst.InitTables<WeiboCreator>();
    }
}
