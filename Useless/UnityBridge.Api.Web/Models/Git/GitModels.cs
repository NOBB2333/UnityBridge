using System;
using System.Collections.Generic;

namespace UnityBridge.Api.Web.Models.Git;

/// <summary>
/// 分支类型
/// </summary>
public enum BranchType
{
    Main,
    Env,
    Tenant
}

/// <summary>
/// 分支信息
/// </summary>
public class BranchInfo
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public bool IsHead { get; set; }
    public BranchType BranchType { get; set; }
    public string LastCommit { get; set; } = string.Empty;
    public string ShortCommit { get; set; } = string.Empty;
    public string? LastMessage { get; set; }
    public string? LastAuthor { get; set; }
    public DateTime LastTime { get; set; }
}

/// <summary>
/// 提交信息
/// </summary>
public class CommitInfo
{
    public string Hash { get; set; } = string.Empty;
    public string ShortHash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Email { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 文件差异
/// </summary>
public class FileDiff
{
    public string Path { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // added, modified, deleted, renamed
    public string? OldPath { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public string? PatchText { get; set; }
}

/// <summary>
/// 分支差异
/// </summary>
public class BranchDiff
{
    public string Base { get; set; } = string.Empty;
    public string Head { get; set; } = string.Empty;
    public int AheadBy { get; set; }
    public int BehindBy { get; set; }
    public List<FileDiff> Files { get; set; } = [];
    public int TotalAdded { get; set; }
    public int TotalDeleted { get; set; }
}

/// <summary>
/// 文件内容
/// </summary>
public class FileContent
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string Commit { get; set; } = string.Empty;
}

/// <summary>
/// 提交请求
/// </summary>
public class CommitRequest
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Author { get; set; }
}

/// <summary>
/// 合并请求
/// </summary>
public class MergeRequest
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Message { get; set; }
}

/// <summary>
/// 同步请求
/// </summary>
public class SyncRequest
{
    public string TenantBranch { get; set; } = string.Empty;
    public List<string>? Files { get; set; }
}
