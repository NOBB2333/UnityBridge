using System.Collections.Generic;
using System.Text.Json.Serialization;
using UnityBridge.Api.Web.Models;
using UnityBridge.Api.Web.Models.Git;
using UnityBridge.Api.Web.Models.Requests;

namespace UnityBridge.Api.Web;

[JsonSerializable(typeof(List<Tenant>))]
[JsonSerializable(typeof(Tenant))]
[JsonSerializable(typeof(Dictionary<string, List<Tenant>>))]
[JsonSerializable(typeof(CreateTenantRequest))]
[JsonSerializable(typeof(UpdateTenantRequest))]
[JsonSerializable(typeof(AddFilesRequest))]
[JsonSerializable(typeof(List<TenantFile>))]
[JsonSerializable(typeof(List<FileDiffInfo>))]
[JsonSerializable(typeof(List<ConfigFile>))]
[JsonSerializable(typeof(CreateFileRequest))]
[JsonSerializable(typeof(UpdateFileRequest))]
[JsonSerializable(typeof(Dictionary<string, List<ConfigFile>>))]
[JsonSerializable(typeof(List<FileHistory>))]
[JsonSerializable(typeof(List<BranchInfo>))]
[JsonSerializable(typeof(CreateBranchRequest))]
[JsonSerializable(typeof(CreateTenantBranchRequest))]
[JsonSerializable(typeof(FileContent))]
[JsonSerializable(typeof(CommitRequest))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(BranchDiff))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, int>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
