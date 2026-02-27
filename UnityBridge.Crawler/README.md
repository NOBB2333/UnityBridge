# UnityBridge.Crawler

`UnityBridge.Crawler` 是 UnityBridge 的多平台爬虫命令行程序，定位是“SDK 之上的工具层封装”。

它基于各平台 SDK（XHS/Bili/Douyin/Kuaishou/Zhihu/Weibo/Tieba），提供统一可执行命令，适合日常抓取、登录检测与数据入库。

## 环境要求

- .NET 10 (`net10.0`)
- 推荐使用 `mise` 管理 SDK 版本

```bash
mise use dotnet
mise exec -- dotnet build /Users/wong/Code/PythonLang/CheckDiff/UnityBridge/UnityBridge.Crawler/UnityBridge.Crawler.csproj
```

## 配置文件

默认读取：

- `UnityBridge.Crawler/Configuration/appsettings.json`

核心配置项：

- `Crawler.SignServerUrl`：签名服务地址（需要签名的平台会使用）
- `Crawler.Database`：SQLite/MySQL
- `Crawler.DefaultDelay`：搜索翻页随机延迟
- `Crawler.MaxPages`：默认最大页数
- `Crawler.Platforms.*.Cookies`：各平台 Cookie

## 命令总览

```bash
UnityBridge.Crawler search <关键词> [平台] [--max-pages 10]
UnityBridge.Crawler detail <平台> <内容ID> [参数]
UnityBridge.Crawler comments <平台> <内容ID> [参数]
UnityBridge.Crawler creator <平台> <创作者ID> [参数]
UnityBridge.Crawler homefeed [平台] [--count 12]
UnityBridge.Crawler login <平台> [--method qr] [--write-config true]
UnityBridge.Crawler login-check [平台]
```

平台可选：

- `all` `xhs` `bili` `douyin` `tieba` `kuaishou` `zhihu` `weibo`

## 常用示例

```bash
# 搜索
UnityBridge.Crawler search "人工智能" all
UnityBridge.Crawler search "Python教程" bili --max-pages 5

# 详情
UnityBridge.Crawler detail douyin 7495763494292188425
UnityBridge.Crawler detail xhs 66f0xxxxxxxxxxxx --xsec-token <token>
UnityBridge.Crawler detail zhihu 1888xxxx --type answer --question-id 6598xxxx

# 评论
UnityBridge.Crawler comments bili 114514 --max-pages 5 --include-sub true
UnityBridge.Crawler comments tieba 123456789 --page 1 --parent-comment-id <pid> --tieba-id <fid>

# 创作者
UnityBridge.Crawler creator xhs 5f6a8a0f0000000001001234 --max-pages 3
UnityBridge.Crawler creator weibo 123456 --container-id 107603123456 --max-pages 2

# 首页流
UnityBridge.Crawler homefeed xhs --count 20
UnityBridge.Crawler homefeed kuaishou --count 12

# 登录（当前支持 B 站二维码）
UnityBridge.Crawler login bili --method qr --write-config true

# 登录检测
UnityBridge.Crawler login-check all
UnityBridge.Crawler login-check bili
```

> 说明：二维码会在终端直接输出，同时会打印登录 URL 作为兜底；当前不依赖 PNG 文件。

## 平台参数说明（关键差异）

- `xhs detail/comments/creator`：建议传 `--xsec-token`
- `zhihu detail`：`answer` 类型必须传 `--question-id`
- `zhihu comments`：可传 `--content-type answer|article|zvideo`
- `weibo creator`：可传 `--container-id`（不传会尝试自动解析）
- `tieba comments`：抓子评论需要 `--parent-comment-id` + `--tieba-id`

## 数据存储

- 默认 SQLite：`crawler.db`
- 支持 MySQL（改 `Crawler.Database.Type` 和连接串）
- 常见表：`*_videos` `*_comments` `*_creators` `xhs_notes` `zhihu_contents` `weibo_notes` 等

## 代码组织

- 命令入口：`Program.cs`
- 命令分平台封装：`Commands/Platforms/CrawlerCommand.<Platform>.cs`
- 客户端工厂：`CrawlerFactory.cs`
- 配置模型：`CrawlerOptions.cs`
- 存储初始化：`CrawlerStorageHelper.cs`
