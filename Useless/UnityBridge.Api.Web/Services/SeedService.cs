using System;
using System.Collections.Generic;
using System.Linq;
using UnityBridge.Api.Web.Models;
using UnityBridge.Api.Web.Models.Git;

namespace UnityBridge.Api.Web.Services;

/// <summary>
/// 种子数据服务 - 初始化 Mock 数据
/// </summary>
public static class SeedService
{
    /// <summary>
    /// 注入 Mock 数据 (可在生产环境中注释掉此调用)
    /// </summary>
    public static void SeedData(DbService db, GitService git)
    {
        var count = db.Db.Queryable<ConfigFile>().Count();
        if (count > 0)
        {
            // 数据已存在，清空重建
            db.Db.Deleteable<TenantFile>().ExecuteCommand();
            db.Db.Deleteable<Tenant>().ExecuteCommand();
            db.Db.Deleteable<ConfigFile>().ExecuteCommand();
        }

        Console.WriteLine("Initializing Mock Data (Multi-Environment)...");

        // 1. 创建多环境配置文件
        var files = new List<ConfigFile>
        {
            // [生产环境 Production] (基础办公套件)
            new() { Name = "智能客服机器人", Path = "chatflows/customer-service.yml", Category = FileCategory.Chatflow, Environment = "production", Version = "2024-05-20", DifyAppId = "prod-cs-001" },
            new() { Name = "销售话术助手", Path = "chatflows/sales-assistant.yml", Category = FileCategory.Chatflow, Environment = "production", Version = "2024-04-15", DifyAppId = "prod-sales-002" },
            new() { Name = "HR招聘面试助手", Path = "chatflows/hr-interviewer.yml", Category = FileCategory.Chatflow, Environment = "production", Version = "2024-03-30", DifyAppId = "prod-hr-005" },
            new() { Name = "企业知识库问答", Path = "chatflows/enterprise-kb.yml", Category = FileCategory.Chatflow, Environment = "production", Version = "2024-05-01", DifyAppId = "prod-kb-008" },

            // [生产环境 Production] (工作流)
            new() { Name = "发票自动处理", Path = "workflows/invoice-processing.yml", Category = FileCategory.Workflow, Environment = "production", Version = "2024-06-01", DifyAppId = "prod-inv-003" },
            new() { Name = "日报自动汇总", Path = "workflows/daily-report-summary.yml", Category = FileCategory.Workflow, Environment = "production", Version = "2024-02-28", DifyAppId = "prod-rpt-004" },
            new() { Name = "合同风险审查", Path = "workflows/contract-review.yml", Category = FileCategory.Workflow, Environment = "production", Version = "2024-05-15", DifyAppId = "prod-ctr-009" },

            // [生产环境 Production] (插件)
            new() { Name = "邮件发送插件", Path = "plugins/email-sender.yml", Category = FileCategory.Plugin, Environment = "production", Version = "2023-12-01" },
            new() { Name = "飞书通知插件", Path = "plugins/feishu-notify.yml", Category = FileCategory.Plugin, Environment = "production", Version = "2024-01-10" },
            new() { Name = "钉钉群机器人", Path = "plugins/dingtalk-bot.yml", Category = FileCategory.Plugin, Environment = "production", Version = "2024-01-15" },

            // [测试环境 Test] (新特性尝鲜)
            new() { Name = "智能客服-Beta (GPT-5)", Path = "chatflows/customer-service-beta.yml", Category = FileCategory.Chatflow, Environment = "test", Version = "2024-06-15-beta", DifyAppId = "test-cs-001" },
            new() { Name = "视频生成工作流 (Sora)", Path = "workflows/video-gen.yml", Category = FileCategory.Workflow, Environment = "test", Version = "2024-06-10-alpha", DifyAppId = "test-vid-002" },
            new() { Name = "代码审计Agent", Path = "agents/code-auditor.yml", Category = FileCategory.Chatflow, Environment = "test", Version = "2024-06-18-dev", DifyAppId = "test-code-003" },

            // [预生产 Pre-prod] (准上线稳定版)
            new() { Name = "智能客服-RC", Path = "chatflows/customer-service-rc.yml", Category = FileCategory.Chatflow, Environment = "preprod", Version = "2024-06-01-rc1", DifyAppId = "pre-cs-001" },
            new() { Name = "月度财报分析-RC", Path = "workflows/finance-analysis-rc.yml", Category = FileCategory.Workflow, Environment = "preprod", Version = "2024-05-25-rc2", DifyAppId = "pre-fin-002" },

            // [迟总一体机 All-in-One] (私有化部署专用)
            new() { Name = "离线知识库助手", Path = "chatflows/offline-kb-bot.yml", Category = FileCategory.Chatflow, Environment = "allinone", Version = "2024-01-01-lts", DifyAppId = "aio-kb-001" },
            new() { Name = "局域网控制插件", Path = "plugins/lan-control.yml", Category = FileCategory.Plugin, Environment = "allinone", Version = "2023-11-20" },
            new() { Name = "工业设备监控流", Path = "workflows/iot-monitor.yml", Category = FileCategory.Workflow, Environment = "allinone", Version = "2024-03-10", DifyAppId = "aio-iot-003" },
        };
        db.Db.Insertable(files).ExecuteCommand();

        // 重新查询获取带 ID 的数据
        files = db.Db.Queryable<ConfigFile>().ToList();

        // 2. 创建租户 (覆盖各行各业)
        var tenants = new List<Tenant>
        {
            // 能源/化工
            new() { Name = "华兴科技集团", Branch = "tenant/huaxing", Notes = "大型国企，主要业务为能源化工，对数据安全要求高", DifyUrl = "https://dify.huaxing.com" },
            new() { Name = "中石油西北分公司", Branch = "tenant/cnpc-nw", Notes = "能源勘探，使用私有化部署", DifyUrl = "http://10.0.0.5:8080" },

            // 互联网/科技
            new() { Name = "蓝鲸创新", Branch = "tenant/blue-whale", Notes = "互联网独角兽，AI赛道，喜欢尝试新功能", DifyUrl = "https://ai.bluewhale.tech" },
            new() { Name = "比特跳动", Branch = "tenant/byte-dance", Notes = "短视频巨头，关注视频生成能力", DifyUrl = "https://dify.bytedance.com" },
            new() { Name = "腾讯云深圳团队", Branch = "tenant/tencent-sz", Notes = "云计算部门，内部工具链集成", DifyUrl = "https://dify.oa.com" },

            // 金融
            new() { Name = "长城证券", Branch = "tenant/great-wall", Notes = "金融证券行业，合规性第一", DifyUrl = "https://copilot.gwsec.com" },
            new() { Name = "招商银行信用卡中心", Branch = "tenant/cmb-cc", Notes = "客服场景重度用户", DifyUrl = "https://ai.cmbchina.com" },

            // 物流/制造
            new() { Name = "顺丰速运", Branch = "tenant/sf-express", Notes = "物流龙头，自动化流程需求多", DifyUrl = "https://dify.sf-express.com" },
            new() { Name = "大疆创新", Branch = "tenant/dji", Notes = "无人机制造，全球化业务", DifyUrl = "https://dify.dji.com" },

            // 零售/消费
            new() { Name = "瑞幸咖啡", Branch = "tenant/luckin", Notes = "新零售，营销文案生成", DifyUrl = "https://ai.luckincoffee.com" },
        };
        db.Db.Insertable(tenants).ExecuteCommand();

        // 重新查询获取带 ID 的数据
        tenants = db.Db.Queryable<Tenant>().ToList();

        // 3. 关联文件 (模拟复杂的使用场景)
        var tenantFiles = new List<TenantFile>
        {
            // 华兴 (保守，用生产环境稳定版)
            new() { TenantId = tenants[0].Id, FileId = files[0].Id, Version = "2024-05-20", Customized = false }, // 客服
            new() { TenantId = tenants[0].Id, FileId = files[4].Id, Version = "2024-06-01", Customized = true },  // 发票 (魔改过)

            // 蓝鲸 (激进，用测试环境)
            new() { TenantId = tenants[2].Id, FileId = files[10].Id, Version = "2024-06-15-beta", Customized = false }, // Beta功能
            new() { TenantId = tenants[2].Id, FileId = files[11].Id, Version = "2024-06-10-alpha", Customized = false }, // 视频生成

            // 中石油 (私有化，用一体机版)
            new() { TenantId = tenants[1].Id, FileId = files[15].Id, Version = "2024-01-01-lts", Customized = false },
            new() { TenantId = tenants[1].Id, FileId = files[17].Id, Version = "2024-03-10", Customized = true }, // 监控流 (定制了设备协议)

            // 招商银行 (用旧版本，不敢升级)
            new() { TenantId = tenants[6].Id, FileId = files[0].Id, Version = "2023-12-01", Customized = false }, // 客服 (很老的版本)

            // 瑞幸咖啡 (正常使用)
            new() { TenantId = tenants[9].Id, FileId = files[1].Id, Version = "2024-04-15", Customized = false }, // 销售话术
        };
        db.Db.Insertable(tenantFiles).ExecuteCommand();

        // 4. 初始化 Git 仓库提交历史
        InitGitData(git, files, tenants);

        Console.WriteLine("Mock Data Initialized (Expanded with Git History)!");
    }

    private static void InitGitData(GitService git, List<ConfigFile> files, List<Tenant> tenants)
    {
        try
        {
            // 1. 确保环境分支存在
            var envs = new[] { "main", "test", "preprod", "allinone" };
            foreach (var env in envs)
            {
                if (env == "main") continue;
                try { git.CreateBranch(env, "main"); } catch { /* 忽略已存在 */ }
            }

            // 2. 为每个环境分支创建基础文件和提交记录
            foreach (var env in envs)
            {
                var envFiles = files.Where(f =>
                    f.Environment == env || (env == "main" && f.Environment == "production")).ToList();

                foreach (var f in envFiles)
                {
                    // 模拟 3 次提交历史
                    for (var i = 0; i < 3; i++)
                    {
                        var content = $"""
                            # {f.Name}
                            # Version: {f.Version}-v{i}
                            # Environment: {env}

                            key: value-{i}
                            timestamp: {DateTime.UtcNow.AddDays(-(10 - i)):O}
                            """;

                        var msg = i == 0 ? $"Initialize {f.Name}" : $"Update {f.Name} to v{i + 1}";
                        var branch = env == "production" ? "main" : env;

                        try
                        {
                            var request = new CommitRequest
                            {
                                Path = f.Path,
                                Content = content,
                                Message = msg,
                                Author = "Admin"
                            };
                            git.CommitFile(branch, request);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to commit {f.Name} to {branch}: {ex.Message}");
                        }
                    }
                }
            }

            // 3. 为租户创建分支并添加定制文件
            foreach (var t in tenants)
            {
                var branchName = string.IsNullOrEmpty(t.Branch) ? $"tenant/{t.Name}" : t.Branch;
                try { git.CreateBranch(branchName, "main"); } catch { /* 忽略 */ }
            }

            Console.WriteLine("Git repository initialized with mock commits.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to initialize Git data: {ex.Message}");
        }
    }
}
