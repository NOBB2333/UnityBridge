using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using UnityBridge.Api.Web.Models;
using UnityBridge.Api.Web.Models.Requests;
using UnityBridge.Api.Web.Services;

namespace UnityBridge.Api.Web.Endpoints;

internal static class TenantsEndpoints
{
    public static void MapTenantsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants").WithTags("Tenants");

        group.MapGet("/", List);
        group.MapGet("/tree", ListTree);
        group.MapGet("/{id:int}", Get);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
        group.MapGet("/{id:int}/files", GetFiles);
        group.MapPost("/{id:int}/files", AddFile);
        group.MapDelete("/{id:int}/files/{fileId:int}", RemoveFile);
        group.MapGet("/{id:int}/files/download", DownloadAllFiles);
        group.MapGet("/{id:int}/files/{fileId:int}/download", DownloadFile);
        group.MapGet("/{id:int}/diff", GetDiff);
        // UploadFile is complex due to IFormFile, skipping for now or implementing later if needed (Minimal API supports IFormFile)
        group.MapPost("/{id:int}/files/{fileId:int}/upload", UploadFile).DisableAntiforgery();
    }

    private static async Task<IResult> List(DbService db)
    {
        var tenants = await db.GetTenantsAsync();
        return Results.Ok(tenants);
    }

    private static async Task<IResult> ListTree(DbService db)
    {
        var tenants = await db.GetTenantsAsync();
        var tree = tenants
            .Where(t => !string.IsNullOrEmpty(t.Name))
            .GroupBy(t => t.Name[0].ToString())
            .ToDictionary(g => g.Key, g => g.ToList());
        return Results.Ok(new { tenants, tree });
    }

    private static async Task<IResult> Get(uint id, DbService db)
    {
        var tenant = await db.GetTenantWithFilesAsync(id);
        return tenant == null ? Results.NotFound(new { error = "tenant not found" }) : Results.Ok(tenant);
    }

    private static async Task<IResult> Create(CreateTenantRequest input, DbService db, GitService git)
    {
        var branchName = $"tenant/{input.Name}";
        var tenant = new Tenant
        {
            Name = input.Name,
            Branch = branchName,
            Notes = input.Notes,
            DifyUrl = input.DifyUrl
        };

        tenant.Id = await db.CreateTenantAsync(tenant);

        var branchCreated = false;
        try
        {
            git.CreateTenantBranch(input.Name);
            branchCreated = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to create git branch for tenant {input.Name}: {ex.Message}");
        }

        return Results.Created($"/api/tenants/{tenant.Id}", new { tenant, branch_created = branchCreated });
    }

    private static async Task<IResult> Update(uint id, UpdateTenantRequest input, DbService db)
    {
        var tenant = await db.GetTenantAsync(id);
        if (tenant == null) return Results.NotFound(new { error = "tenant not found" });

        if (!string.IsNullOrEmpty(input.Name))
        {
            tenant.Name = input.Name;
            tenant.Branch = $"tenant/{input.Name}";
        }
        if (input.Notes != null) tenant.Notes = input.Notes;
        if (input.DifyUrl != null) tenant.DifyUrl = input.DifyUrl;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.UpdateTenantAsync(tenant);
        return Results.Ok(tenant);
    }

    private static async Task<IResult> Delete(uint id, DbService db, GitService git)
    {
        var tenant = await db.GetTenantAsync(id);
        if (tenant == null) return Results.NotFound(new { error = "tenant not found" });

        await db.DeleteTenantAsync(id);

        var branchDeleted = false;
        if (!string.IsNullOrEmpty(tenant.Branch))
        {
            try
            {
                git.DeleteBranch(tenant.Branch);
                branchDeleted = true;
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Failed to delete git branch: {ex.Message}"); }
        }

        return Results.Ok(new { message = "deleted", branch_deleted = branchDeleted });
    }

    private static async Task<IResult> GetFiles(uint id, DbService db)
    {
        try
        {
            return Results.Ok(await db.GetTenantFilesAsync(id));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting files for tenant {id}: {ex}");
            return Results.InternalServerError(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AddFile(uint id, AddFilesRequest input, DbService db)
    {
        var items = new List<(uint FileId, string? Version, bool Customized)>();
        if (input.Items is { Count: > 0 }) items.AddRange(input.Items.Select(i => (i.FileId, i.Version, i.Customized)));
        else if (input.FileIds is { Count: > 0 }) items.AddRange(input.FileIds.Select(fid => (fid, (string?)null, input.Customized)));

        if (items.Count == 0) return Results.BadRequest(new { error = "no files to add" });

        var added = await db.AddTenantFilesAsync(id, items);
        return Results.Created("", new { added });
    }

    private static async Task<IResult> RemoveFile(uint id, uint fileId, DbService db)
    {
        await db.RemoveTenantFileAsync(id, fileId);
        return Results.Ok(new { message = "removed" });
    }

    private static async Task<IResult> DownloadAllFiles(uint id, DbService db)
    {
        var tenant = await db.GetTenantAsync(id);
        if (tenant == null) return Results.NotFound(new { error = "tenant not found" });

        var tenantFiles = await db.GetTenantFilesAsync(id);

        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var tf in tenantFiles)
            {
                if (tf.File == null) continue;
                var content = $"# {tf.File.Name}\n# Category: {tf.File.Category}\n# Tenant: {tenant.Name}\n\nname: {tf.File.Name}\nversion: {tf.Version}\n";
                var path = !string.IsNullOrEmpty(tf.File.Path) ? tf.File.Path : $"{tf.File.Name}.yml";
                var entry = zip.CreateEntry(path);
                await using var writer = new StreamWriter(entry.Open());
                await writer.WriteAsync(content);
            }
        }
        ms.Position = 0;
        return Results.File(ms.ToArray(), "application/zip", $"{tenant.Name}-configs.zip");
    }

    private static async Task<IResult> DownloadFile(uint id, uint fileId, DbService db)
    {
        var tf = await db.GetTenantFileAsync(id, fileId);
        if (tf?.File == null || tf.Tenant == null) return Results.NotFound(new { error = "file not found" });

        var content = $"# {tf.File.Name}\n# Category: {tf.File.Category}\n# Tenant: {tf.Tenant.Name}\n\nname: {tf.File.Name}\nversion: {tf.Version}\n";
        return Results.File(System.Text.Encoding.UTF8.GetBytes(content), "application/x-yaml", tf.File.Name);
    }

    private static async Task<IResult> GetDiff(uint id, DbService db)
    {
        var tenantFiles = await db.GetTenantFilesAsync(id);
        var diffs = tenantFiles.Select(tf => new FileDiffInfo
        {
            FileId = tf.FileId,
            FileName = tf.File?.Name ?? string.Empty,
            MainVersion = tf.File?.Version,
            TenantVersion = tf.Version,
            HasDiff = tf.Customized || tf.Version != tf.File?.Version,
            DiffContent = tf.Customized || tf.Version != tf.File?.Version
                ? $"--- main/{tf.File?.Path} ({tf.File?.Version})\n+++ tenant/{tf.File?.Path} ({tf.Version})\n@@ -10,4 +10,5 @@\n- version: {tf.File?.Version}\n+ version: {tf.Version}\n+ # 定制化配置参数\n+ timeout: 60s\n"
                : null
        }).ToList();
        return Results.Ok(diffs);
    }

    private static async Task<IResult> UploadFile(uint id, uint fileId, IFormFile file, [FromForm] string? message, DbService db, GitService git)
    {
        var tenantFile = await db.GetTenantFileAsync(id, fileId);
        if (tenantFile?.File == null || tenantFile.Tenant == null) return Results.NotFound(new { error = "association not found" });

        message ??= "Update configuration file";
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();

        string? commitHash = null;
        try
        {
            var filePath = !string.IsNullOrEmpty(tenantFile.File.Path) ? tenantFile.File.Path : $"{tenantFile.File.Name}.yml";
            var branchName = !string.IsNullOrEmpty(tenantFile.Tenant.Branch) ? tenantFile.Tenant.Branch : $"tenant/{tenantFile.Tenant.Name}";
            var commitInfo = git.CommitFile(branchName, new Models.Git.CommitRequest
            {
                Path = filePath, Content = content, Message = $"[{tenantFile.Tenant.Name}] {message}", Author = "DCM Upload"
            });
            commitHash = commitInfo.ShortHash;
        }
        catch (Exception ex) { Console.WriteLine($"Warning: Failed to commit: {ex.Message}"); }

        var newVersion = commitHash ?? $"{tenantFile.Version}-dev";
        tenantFile.Customized = true;
        tenantFile.Version = newVersion;
        tenantFile.CustomNote = message;
        await db.UpdateTenantFileAsync(tenantFile);

        return Results.Ok(new { message = "uploaded", version = newVersion, commit_hash = commitHash });
    }
}
