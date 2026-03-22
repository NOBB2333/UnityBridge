using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using UnityBridge.Api.Web.Models.Git;

namespace UnityBridge.Api.Web.Services;

/// <summary>
/// Git 操作服务
/// </summary>
public class GitService : IDisposable
{
    private readonly string _repoPath;
    private Repository? _repo;

    // 预定义的环境分支
    private static readonly HashSet<string> EnvBranches = ["main", "master", "test", "preprod", "allinone"];

    public GitService(string repoPath)
    {
        _repoPath = repoPath;
        InitializeRepository();
    }

    private void InitializeRepository()
    {
        // 确保目录存在
        Directory.CreateDirectory(_repoPath);

        // 尝试打开已有仓库，否则初始化新仓库
        if (Repository.IsValid(_repoPath))
        {
            _repo = new Repository(_repoPath);
        }
        else
        {
            Repository.Init(_repoPath);
            _repo = new Repository(_repoPath);
            CreateInitialCommit();
        }
    }

    private void CreateInitialCommit()
    {
        var readmePath = Path.Combine(_repoPath, "README.md");
        var content = "# DCM Configuration Repository\n\nThis repository contains Dify configuration files.\n";
        File.WriteAllText(readmePath, content);

        Commands.Stage(_repo!, "README.md");

        var signature = new Signature("DCM System", "admin@dcm.local", DateTimeOffset.Now);
        _repo!.Commit("Initial commit", signature, signature);

        // Ensure main branch exists (rename master if needed)
        var master = _repo.Branches["master"];
        if (master != null && _repo.Branches["main"] == null)
        {
            _repo.Branches.Rename(master, "main");
        }
    }

    /// <summary>
    /// 获取当前分支名
    /// </summary>
    public string GetCurrentBranch()
    {
        return _repo?.Head?.FriendlyName ?? "main";
    }

    /// <summary>
    /// 获取所有分支
    /// </summary>
    public List<BranchInfo> ListBranches()
    {
        var branches = new List<BranchInfo>();
        var currentBranch = GetCurrentBranch();

        foreach (var branch in _repo!.Branches.Where(b => !b.IsRemote))
        {
            var name = branch.FriendlyName;
            var tip = branch.Tip;

            branches.Add(new BranchInfo
            {
                Name = name,
                ShortName = GetShortName(name),
                IsHead = name == currentBranch,
                BranchType = GetBranchType(name),
                LastCommit = tip?.Sha ?? string.Empty,
                ShortCommit = tip?.Sha[..7] ?? string.Empty,
                LastMessage = tip?.MessageShort,
                LastAuthor = tip?.Author.Name,
                LastTime = tip?.Author.When.DateTime ?? DateTime.MinValue
            });
        }

        // 按类型和名称排序
        return branches
            .OrderBy(b => BranchTypeOrder(b.BranchType))
            .ThenBy(b => b.Name)
            .ToList();
    }

    private static int BranchTypeOrder(BranchType t) => t switch
    {
        BranchType.Main => 0,
        BranchType.Env => 1,
        BranchType.Tenant => 2,
        _ => 3
    };

    private static string GetShortName(string name)
    {
        return name.StartsWith("tenant/") ? name["tenant/".Length..] : name;
    }

    private static BranchType GetBranchType(string name)
    {
        if (name is "main" or "master") return BranchType.Main;
        if (EnvBranches.Contains(name)) return BranchType.Env;
        if (name.StartsWith("tenant/")) return BranchType.Tenant;
        return BranchType.Tenant;
    }

    /// <summary>
    /// 创建新分支
    /// </summary>
    public void CreateBranch(string name, string? fromBranch = null)
    {
        fromBranch ??= "main";

        var sourceBranch = _repo!.Branches[fromBranch]
            ?? _repo.Branches["master"]
            ?? throw new InvalidOperationException($"Source branch not found: {fromBranch}");

        _repo.CreateBranch(name, sourceBranch.Tip);
    }

    /// <summary>
    /// 删除分支
    /// </summary>
    public void DeleteBranch(string name)
    {
        if (EnvBranches.Contains(name))
        {
            throw new InvalidOperationException($"Cannot delete protected branch: {name}");
        }

        _repo!.Branches.Remove(name);
    }

    /// <summary>
    /// 切换分支
    /// </summary>
    public void SwitchBranch(string name)
    {
        var branch = _repo!.Branches[name]
            ?? throw new InvalidOperationException($"Branch not found: {name}");

        Commands.Checkout(_repo, branch);
    }

    /// <summary>
    /// 获取指定分支的文件内容
    /// </summary>
    public FileContent? GetFileContent(string branch, string path)
    {
        var branchRef = _repo!.Branches[branch]
            ?? throw new InvalidOperationException($"Branch not found: {branch}");

        var commit = branchRef.Tip;
        var treeEntry = commit?[path];

        if (treeEntry?.Target is not Blob blob)
        {
            return null;
        }

        return new FileContent
        {
            Path = path,
            Content = blob.GetContentText(),
            Size = blob.Size,
            Branch = branch,
            Commit = commit?.Sha[..7] ?? string.Empty
        };
    }

    /// <summary>
    /// 列出分支下的文件
    /// </summary>
    public List<string> ListFiles(string branch)
    {
        var branchRef = _repo!.Branches[branch]
            ?? throw new InvalidOperationException($"Branch not found: {branch}");

        var files = new List<string>();
        var tree = branchRef.Tip?.Tree;

        if (tree != null)
        {
            CollectFiles(tree, "", files);
        }

        return files;
    }

    private static void CollectFiles(Tree tree, string prefix, List<string> files)
    {
        foreach (var entry in tree)
        {
            var fullPath = string.IsNullOrEmpty(prefix) ? entry.Name : $"{prefix}/{entry.Name}";

            if (entry.Target is Blob)
            {
                files.Add(fullPath);
            }
            else if (entry.Target is Tree subTree)
            {
                CollectFiles(subTree, fullPath, files);
            }
        }
    }

    /// <summary>
    /// 提交文件变更
    /// </summary>
    public CommitInfo CommitFile(string branch, CommitRequest request)
    {
        SwitchBranch(branch);

        // 写入文件
        var fullPath = Path.Combine(_repoPath, request.Path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(fullPath, request.Content);

        // Git Add
        Commands.Stage(_repo!, request.Path);

        // Git Commit
        var author = request.Author ?? "DCM System";
        var signature = new Signature(author, "admin@dcm.local", DateTimeOffset.Now);
        var commit = _repo!.Commit(request.Message, signature, signature);

        return new CommitInfo
        {
            Hash = commit.Sha,
            ShortHash = commit.Sha[..7],
            Message = request.Message,
            Author = author,
            Timestamp = commit.Author.When.DateTime
        };
    }

    /// <summary>
    /// 获取文件的提交历史
    /// </summary>
    public List<CommitInfo> GetFileHistory(string branch, string? path, int limit = 20)
    {
        var branchRef = _repo!.Branches[branch]
            ?? throw new InvalidOperationException($"Branch not found: {branch}");

        var filter = new CommitFilter
        {
            IncludeReachableFrom = branchRef.Tip,
            SortBy = CommitSortStrategies.Time
        };

        var commits = new List<CommitInfo>();
        var count = 0;

        foreach (var commit in _repo.Commits.QueryBy(filter))
        {
            if (count >= limit) break;

            // 如果指定了路径，检查提交是否影响该文件
            if (!string.IsNullOrEmpty(path))
            {
                var parent = commit.Parents.FirstOrDefault();
                if (parent != null)
                {
                    var changes = _repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
                    if (!changes.Any(c => c.Path == path || c.OldPath == path))
                    {
                        continue;
                    }
                }
            }

            commits.Add(new CommitInfo
            {
                Hash = commit.Sha,
                ShortHash = commit.Sha[..7],
                Message = commit.MessageShort,
                Author = commit.Author.Name,
                Email = commit.Author.Email,
                Timestamp = commit.Author.When.DateTime
            });

            count++;
        }

        return commits;
    }

    /// <summary>
    /// 比较两个分支的差异
    /// </summary>
    public BranchDiff DiffBranches(string baseBranch, string headBranch)
    {
        var baseRef = _repo!.Branches[baseBranch]
            ?? throw new InvalidOperationException($"Base branch not found: {baseBranch}");

        var headRef = _repo.Branches[headBranch]
            ?? throw new InvalidOperationException($"Head branch not found: {headBranch}");

        var baseCommit = baseRef.Tip;
        var headCommit = headRef.Tip;

        var changes = _repo.Diff.Compare<TreeChanges>(baseCommit?.Tree, headCommit?.Tree);
        var patch = _repo.Diff.Compare<Patch>(baseCommit?.Tree, headCommit?.Tree);

        var files = new List<FileDiff>();
        var totalAdded = 0;
        var totalDeleted = 0;

        foreach (var change in changes)
        {
            var filePatch = patch[change.Path];
            var diff = new FileDiff
            {
                Path = change.Path,
                Status = change.Status switch
                {
                    ChangeKind.Added => "added",
                    ChangeKind.Deleted => "deleted",
                    ChangeKind.Modified => "modified",
                    ChangeKind.Renamed => "renamed",
                    _ => "unknown"
                },
                OldPath = change.OldPath,
                Additions = filePatch?.LinesAdded ?? 0,
                Deletions = filePatch?.LinesDeleted ?? 0,
                PatchText = filePatch?.Patch
            };

            totalAdded += diff.Additions;
            totalDeleted += diff.Deletions;
            files.Add(diff);
        }

        return new BranchDiff
        {
            Base = baseBranch,
            Head = headBranch,
            Files = files,
            TotalAdded = totalAdded,
            TotalDeleted = totalDeleted
        };
    }

    /// <summary>
    /// 确保环境分支存在
    /// </summary>
    public void EnsureEnvironmentBranches()
    {
        var envs = new[] { "main", "test", "preprod", "allinone" };

        foreach (var env in envs)
        {
            if (_repo!.Branches[env] == null)
            {
                try
                {
                    CreateBranch(env);
                }
                catch
                {
                    // 忽略创建失败
                }
            }
        }
    }

    /// <summary>
    /// 创建租户分支
    /// </summary>
    public void CreateTenantBranch(string tenantName)
    {
        var branchName = $"tenant/{tenantName}";
        CreateBranch(branchName, "main");
    }

    public void Dispose()
    {
        _repo?.Dispose();
        GC.SuppressFinalize(this);
    }
}
