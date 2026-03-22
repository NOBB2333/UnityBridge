using Microsoft.Extensions.DependencyInjection;
using UnityBridge.Crawler.Core.SignService;
using UnityBridge.Crawler.BiliBili;
using UnityBridge.Crawler.Douyin;

namespace UnityBridge.Api.Sdk.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSdkServices(this IServiceCollection services)
    {
        // 注册 HTTP Client
        services.AddHttpClient();
        services.AddMemoryCache();

        // 注册签名服务 (使用本地签名)
        services.AddSingleton<ISignClient, LocalSignClient>();

        // 注册 SDK Clients
        // BiliBili
        services.AddSingleton<BiliClientOptions>(sp => new BiliClientOptions 
        { 
            // 可以在这里配置默认项，或者从配置文件读取
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
        services.AddSingleton<BiliClient>();

        // Douyin
        services.AddSingleton<DouyinClientOptions>(sp => new DouyinClientOptions
        {
             UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
        services.AddSingleton<DouyinClient>();

        // Kuaishou
        services.AddSingleton<UnityBridge.Crawler.Kuaishou.KuaishouClientOptions>(sp => new UnityBridge.Crawler.Kuaishou.KuaishouClientOptions
        {
             UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
        services.AddSingleton<UnityBridge.Crawler.Kuaishou.KuaishouClient>();

        // Tieba
        services.AddSingleton<UnityBridge.Crawler.Tieba.TiebaClientOptions>(sp => new UnityBridge.Crawler.Tieba.TiebaClientOptions
        {
             UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
        services.AddSingleton<UnityBridge.Crawler.Tieba.TiebaClient>();

        // Weibo
        services.AddSingleton<UnityBridge.Crawler.Weibo.WeiboClientOptions>(sp => new UnityBridge.Crawler.Weibo.WeiboClientOptions
        {
             UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
        services.AddSingleton<UnityBridge.Crawler.Weibo.WeiboClient>();

        // XiaoHongShu
        services.AddSingleton<UnityBridge.Crawler.XiaoHongShu.XhsClientOptions>(sp => new UnityBridge.Crawler.XiaoHongShu.XhsClientOptions
        {
             UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
        services.AddSingleton<UnityBridge.Crawler.XiaoHongShu.XhsClient>();

        // Zhihu
        services.AddSingleton<UnityBridge.Crawler.Zhihu.ZhihuClientOptions>(sp => new UnityBridge.Crawler.Zhihu.ZhihuClientOptions
        {
             UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
        services.AddSingleton<UnityBridge.Crawler.Zhihu.ZhihuClient>();

        return services;
    }
}
