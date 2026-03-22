using System.Collections.Generic;
using UnityBridge.Api.Web.Models;

namespace UnityBridge.Api.Web.Models.Requests;

public record LoginRequest(string Username, string Password);

public record CreateTenantRequest(string Name, string? Notes, string? DifyUrl);
public record UpdateTenantRequest(string? Name, string? Notes, string? DifyUrl);
public record AddFileItem(uint FileId, string? Version, bool Customized);
public record AddFilesRequest(List<AddFileItem>? Items, List<uint>? FileIds, bool Customized);

public record CreateFileRequest(string Name, string Path, string? Category, string? Environment, string? DifyAppId);
public record UpdateFileRequest(string? Name, string? Path, string? Category, string? Environment, string? DifyAppId);

public record CreateBranchRequest(string Name, string? FromBranch);
public record CreateTenantBranchRequest(string TenantName);
