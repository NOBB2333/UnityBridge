using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UnityBridge.Api.Web.Models;
using UnityBridge.Api.Web.Models.Requests;
using UnityBridge.Api.Web.Services;

namespace UnityBridge.Api.Web.Endpoints;

internal static class FilesEndpoints
{
    public static void MapFilesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files").WithTags("Files");

        group.MapGet("/", List);
        group.MapGet("/main-repo", ListMainRepo);
        group.MapGet("/categories", GetCategories);
        group.MapGet("/{id:int}", Get);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
        group.MapGet("/{id:int}/history", GetHistory);
        group.MapGet("/{id:int}/who-uses", WhoUses);
    }

    private static async Task<IResult> List(DbService db, string? category = null, string? env = null, bool main_only = false)
    {
        var actualEnv = env;
        if (main_only && string.IsNullOrEmpty(env)) actualEnv = "production";
        return Results.Ok(await db.GetFilesAsync(category, actualEnv));
    }

    private static async Task<IResult> ListMainRepo(string? env, DbService db)
    {
        env ??= "production";
        var files = await db.GetFilesAsync(null, env);
        var byCategory = new Dictionary<string, List<ConfigFile>>
        {
            ["chatflow"] = [], ["workflow"] = [], ["plugin"] = []
        };
        foreach (var file in files)
        {
            var cat = file.Category.ToString().ToLower();
            if (byCategory.TryGetValue(cat, out var list)) list.Add(file);
            else byCategory["workflow"].Add(file);
        }
        return Results.Ok(new { total = files.Count, files, byCategory });
    }

    private static async Task<IResult> GetCategories(DbService db) => Results.Ok(await db.GetCategoryStatsAsync());

    private static async Task<IResult> Get(uint id, DbService db)
    {
        var file = await db.GetFileAsync(id);
        return file == null ? Results.NotFound(new { error = "not found" }) : Results.Ok(file);
    }

    private static async Task<IResult> Create(CreateFileRequest input, DbService db)
    {
        var category = FileCategory.Workflow;
        if (!string.IsNullOrEmpty(input.Category)) Enum.TryParse(input.Category, true, out category);
        var file = new ConfigFile
        {
            Name = input.Name, Path = input.Path, Category = category,
            Environment = input.Environment ?? "production", DifyAppId = input.DifyAppId
        };
        file.Id = await db.CreateFileAsync(file);
        return Results.Created($"/api/files/{file.Id}", file);
    }

    private static async Task<IResult> Update(uint id, UpdateFileRequest input, DbService db)
    {
        var file = await db.GetFileAsync(id);
        if (file == null) return Results.NotFound(new { error = "not found" });

        if (!string.IsNullOrEmpty(input.Name)) file.Name = input.Name;
        if (!string.IsNullOrEmpty(input.Path)) file.Path = input.Path;
        if (!string.IsNullOrEmpty(input.Category) && Enum.TryParse<FileCategory>(input.Category, true, out var cat)) file.Category = cat;
        if (!string.IsNullOrEmpty(input.Environment)) file.Environment = input.Environment;
        if (input.DifyAppId != null) file.DifyAppId = input.DifyAppId;
        file.UpdatedAt = DateTime.UtcNow;

        await db.UpdateFileAsync(file);
        return Results.Ok(file);
    }

    private static async Task<IResult> Delete(uint id, DbService db)
    {
        await db.DeleteFileAsync(id);
        return Results.Ok(new { message = "deleted" });
    }

    private static async Task<IResult> GetHistory(uint id, DbService db) => Results.Ok(await db.GetFileHistoryAsync(id));

    private static async Task<IResult> WhoUses(uint id, DbService db) => Results.Ok(await db.GetFileUsersAsync(id));
}
