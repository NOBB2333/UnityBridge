using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UnityBridge.Api.Web.Models.Git;
using UnityBridge.Api.Web.Models.Requests;
using UnityBridge.Api.Web.Services;

namespace UnityBridge.Api.Web.Endpoints;

internal static class GitEndpoints
{
    public static void MapGitEndpoints(this IEndpointRouteBuilder app)
    {
        var branchGroup = app.MapGroup("/api/branches").WithTags("Branches");
        branchGroup.MapGet("/", ListBranches);
        branchGroup.MapGet("/current", GetCurrentBranch);
        branchGroup.MapPost("/", CreateBranch);
        branchGroup.MapPost("/tenant", CreateTenantBranch);
        branchGroup.MapPost("/ensure-env", EnsureEnvBranches);
        branchGroup.MapDelete("/{name}", DeleteBranch);
        branchGroup.MapPost("/{name}/switch", SwitchBranch);

        var gitGroup = app.MapGroup("/api/git").WithTags("Git");
        gitGroup.MapGet("/files", ListFiles);
        gitGroup.MapGet("/file", GetFileContent);
        gitGroup.MapPost("/commit", CommitFile);
        gitGroup.MapGet("/history", GetHistory);
        gitGroup.MapGet("/diff", DiffBranches);
    }

    private static IResult ListBranches(GitService git) => Results.Ok(git.ListBranches());

    private static IResult GetCurrentBranch(GitService git) => Results.Ok(new { current_branch = git.GetCurrentBranch() });

    private static IResult CreateBranch(CreateBranchRequest input, GitService git)
    {
        git.CreateBranch(input.Name, input.FromBranch);
        return Results.Created("", new { message = "branch created", name = input.Name });
    }

    private static IResult CreateTenantBranch(CreateTenantBranchRequest input, GitService git)
    {
        git.CreateTenantBranch(input.TenantName);
        return Results.Created("", new { message = "tenant branch created", branch = $"tenant/{input.TenantName}" });
    }

    private static IResult EnsureEnvBranches(GitService git)
    {
        git.EnsureEnvironmentBranches();
        return Results.Ok(new { message = "environment branches ensured" });
    }

    private static IResult DeleteBranch(string name, GitService git)
    {
        git.DeleteBranch(name);
        return Results.Ok(new { message = "branch deleted" });
    }

    private static IResult SwitchBranch(string name, GitService git)
    {
        git.SwitchBranch(name);
        return Results.Ok(new { message = "switched", branch = name });
    }

    private static IResult ListFiles(string? branch, GitService git)
    {
        branch ??= "main";
        return Results.Ok(new { branch, files = git.ListFiles(branch) });
    }

    private static IResult GetFileContent(string? branch, string? path, GitService git)
    {
        if (string.IsNullOrEmpty(path)) return Results.BadRequest(new { error = "path required" });
        var content = git.GetFileContent(branch ?? "main", path);
        return content == null ? Results.NotFound(new { error = "not found" }) : Results.Ok(content);
    }

    private static IResult CommitFile(string? branch, CommitRequest req, GitService git)
    {
        return Results.Created("", git.CommitFile(branch ?? "main", req));
    }

    private static IResult GetHistory(string? branch, string? path, int limit, GitService git)
    {
        if (limit == 0) limit = 20;
        return Results.Ok(git.GetFileHistory(branch ?? "main", path, limit));
    }

    private static IResult DiffBranches(string? @base, string? head, GitService git)
    {
        if (string.IsNullOrEmpty(head)) return Results.BadRequest(new { error = "head branch required" });
        return Results.Ok(git.DiffBranches(@base ?? "main", head));
    }
}
