using System;
using System.Collections.Generic;
using SqlSugar;

namespace UnityBridge.Api.Web.Models;

/// <summary>
/// 文件分类
/// </summary>
public enum FileCategory
{
    Chatflow,
    Workflow,
    Plugin
}

/// <summary>
/// 租户信息
/// </summary>
public class Tenant
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    [SugarColumn(IsNullable = true)]
    public string? Notes { get; set; }
    [SugarColumn(IsNullable = true)]
    public string? DifyUrl { get; set; }
    [SugarColumn(IsNullable = true)]
    public string? RepoPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
    [SugarColumn(IsNullable = true)]
    public DateTime? DeletedAt { get; set; }

    // 关联 (忽略)
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(TenantFile.TenantId))]
    public List<TenantFile>? Files { get; set; }
}

/// <summary>
/// 配置文件
/// </summary>
public class ConfigFile
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public FileCategory Category { get; set; } = FileCategory.Workflow;
    public string Environment { get; set; } = "production";
    [SugarColumn(IsNullable = true)]
    public string? DifyAppId { get; set; }
    public string Version { get; set; } = "v1.0.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
    [SugarColumn(IsNullable = true)]
    public DateTime? DeletedAt { get; set; }

    // 计算字段 (非持久化)
    [SugarColumn(IsIgnore = true)]
    public int UsageCount { get; set; }
}

/// <summary>
/// 租户-文件关联
/// </summary>
public class TenantFile
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public uint Id { get; set; }
    public uint TenantId { get; set; }
    public uint FileId { get; set; }
    public bool Customized { get; set; }
    [SugarColumn(IsNullable = true)]
    public string? CustomNote { get; set; }
    [SugarColumn(IsNullable = true)]
    public string? Version { get; set; }
    [SugarColumn(IsNullable = true)]
    public string? LocalPath { get; set; }

    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(TenantId))]
    public Tenant? Tenant { get; set; }
    
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(FileId))]
    public ConfigFile? File { get; set; }
}

/// <summary>
/// 文件版本历史
/// </summary>
public class FileHistory
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public uint Id { get; set; }
    public uint FileId { get; set; }
    [SugarColumn(IsNullable = true)]
    public uint? TenantId { get; set; }
    public string CommitSha { get; set; } = string.Empty;
    [SugarColumn(IsNullable = true)]
    public string? Message { get; set; }
    [SugarColumn(IsNullable = true)]
    public string? Author { get; set; }
    public DateTime Timestamp { get; set; }

    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(FileId))]
    public ConfigFile? File { get; set; }
    
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(TenantId))]
    public Tenant? Tenant { get; set; }
}

/// <summary>
/// 仓库配置
/// </summary>
public class RepoConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    [SugarColumn(IsNullable = true)]
    public string? RemoteUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
    [SugarColumn(IsNullable = true)]
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// 文件差异 (非持久化)
/// </summary>
public class FileDiffInfo
{
    public uint FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? MainVersion { get; set; }
    public string? TenantVersion { get; set; }
    public bool HasDiff { get; set; }
    public string? DiffContent { get; set; }
}

