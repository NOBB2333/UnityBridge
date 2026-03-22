using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;
using UnityBridge.Api.Web.Models;

namespace UnityBridge.Api.Web.Services;

/// <summary>
/// 数据库服务
/// </summary>
public class DbService : IDisposable
{
    private readonly ISqlSugarClient _db;

    public DbService(string dbPath)
    {
        _db = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = $"DataSource={dbPath}",
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        // 自动建表
        _db.CodeFirst.InitTables<Tenant, ConfigFile, TenantFile, FileHistory, RepoConfig>();
    }

    public ISqlSugarClient Db => _db;

    #region Tenant Operations

    public async Task<List<Tenant>> GetTenantsAsync()
    {
        return await _db.Queryable<Tenant>().ToListAsync();
    }

    public async Task<Tenant?> GetTenantAsync(uint id)
    {
        return await _db.Queryable<Tenant>().FirstAsync(t => t.Id == id);
    }

    public async Task<Tenant?> GetTenantWithFilesAsync(uint id)
    {
        var tenant = await _db.Queryable<Tenant>().FirstAsync(t => t.Id == id);
        if (tenant != null)
        {
            tenant.Files = await _db.Queryable<TenantFile>()
                .Includes(tf => tf.File)
                .Where(tf => tf.TenantId == id)
                .ToListAsync();
        }
        return tenant;
    }

    public async Task<uint> CreateTenantAsync(Tenant tenant)
    {
        return (uint)await _db.Insertable(tenant).ExecuteReturnIdentityAsync();
    }

    public async Task<bool> UpdateTenantAsync(Tenant tenant)
    {
        return await _db.Updateable(tenant).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> DeleteTenantAsync(uint id)
    {
        return await _db.Deleteable<Tenant>().Where(t => t.Id == id).ExecuteCommandAsync() > 0;
    }

    #endregion

    #region ConfigFile Operations

    public async Task<List<ConfigFile>> GetFilesAsync(string? category = null, string? env = null)
    {
        var query = _db.Queryable<ConfigFile>();

        if (!string.IsNullOrEmpty(category))
        {
            var cat = Enum.Parse<FileCategory>(category, true);
            query = query.Where(f => f.Category == cat);
        }

        if (!string.IsNullOrEmpty(env) && env != "all")
        {
            query = query.Where(f => f.Environment == env);
        }

        var files = await query.ToListAsync();

        // 获取使用计数
        foreach (var file in files)
        {
            file.UsageCount = await _db.Queryable<TenantFile>().CountAsync(tf => tf.FileId == file.Id);
        }

        return files;
    }

    public async Task<ConfigFile?> GetFileAsync(uint id)
    {
        var file = await _db.Queryable<ConfigFile>().FirstAsync(f => f.Id == id);
        if (file != null)
        {
            file.UsageCount = await _db.Queryable<TenantFile>().CountAsync(tf => tf.FileId == id);
        }
        return file;
    }

    public async Task<uint> CreateFileAsync(ConfigFile file)
    {
        return (uint)await _db.Insertable(file).ExecuteReturnIdentityAsync();
    }

    public async Task<bool> UpdateFileAsync(ConfigFile file)
    {
        return await _db.Updateable(file).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> DeleteFileAsync(uint id)
    {
        return await _db.Deleteable<ConfigFile>().Where(f => f.Id == id).ExecuteCommandAsync() > 0;
    }

    #endregion

    #region TenantFile Operations

    public async Task<List<TenantFile>> GetTenantFilesAsync(uint tenantId)
    {
        return await _db.Queryable<TenantFile>()
            .Includes(tf => tf.File)
            .Where(tf => tf.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<TenantFile?> GetTenantFileAsync(uint tenantId, uint fileId)
    {
        return await _db.Queryable<TenantFile>()
            .Includes(tf => tf.File)
            .Includes(tf => tf.Tenant)
            .FirstAsync(tf => tf.TenantId == tenantId && tf.FileId == fileId);
    }

    public async Task<int> AddTenantFilesAsync(uint tenantId, List<(uint FileId, string? Version, bool Customized)> items)
    {
        var added = 0;
        foreach (var item in items)
        {
            // 检查是否已存在
            var exists = await _db.Queryable<TenantFile>()
                .AnyAsync(tf => tf.TenantId == tenantId && tf.FileId == item.FileId);

            if (exists) continue;

            var version = item.Version;
            if (string.IsNullOrEmpty(version))
            {
                var file = await _db.Queryable<ConfigFile>().FirstAsync(f => f.Id == item.FileId);
                version = file?.Version;
            }

            await _db.Insertable(new TenantFile
            {
                TenantId = tenantId,
                FileId = item.FileId,
                Version = version,
                Customized = item.Customized
            }).ExecuteCommandAsync();

            added++;
        }
        return added;
    }

    public async Task<bool> RemoveTenantFileAsync(uint tenantId, uint fileId)
    {
        return await _db.Deleteable<TenantFile>()
            .Where(tf => tf.TenantId == tenantId && tf.FileId == fileId)
            .ExecuteCommandAsync() > 0;
    }

    public async Task<bool> UpdateTenantFileAsync(TenantFile tenantFile)
    {
        return await _db.Updateable(tenantFile).ExecuteCommandAsync() > 0;
    }

    #endregion

    #region FileHistory Operations

    public async Task<List<FileHistory>> GetFileHistoryAsync(uint fileId)
    {
        return await _db.Queryable<FileHistory>()
            .Where(h => h.FileId == fileId)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync();
    }

    #endregion

    #region Stats

    public async Task<List<object>> GetCategoryStatsAsync()
    {
        return await _db.Queryable<ConfigFile>()
            .GroupBy(f => f.Category)
            .Select(f => new { Category = f.Category.ToString(), Count = SqlFunc.AggregateCount(f.Id) } as object)
            .ToListAsync();
    }

    public async Task<List<object>> GetFileUsersAsync(uint fileId)
    {
        var tenantFiles = await _db.Queryable<TenantFile>()
            .Includes(tf => tf.Tenant)
            .Where(tf => tf.FileId == fileId)
            .ToListAsync();

        return tenantFiles.Select(tf => new
        {
            Tenant = tf.Tenant,
            Customized = tf.Customized,
            Version = tf.Version
        } as object).ToList();
    }

    #endregion

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
