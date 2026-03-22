using Microsoft.AspNetCore.Mvc;
using UnityBridge.Crawler.Zhihu;
using System.Text.Json.Serialization;

namespace UnityBridge.Api.Sdk.Endpoints;

public static class ZhihuEndpoints
{
    public static void MapZhihuEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/zhihu").WithTags("Zhihu");

        group.MapPost("/request", async (
            [FromServices] ZhihuClient client, 
            [FromBody] ZhihuProxyRequest request,
            CancellationToken ct) =>
        {
            var result = await client.SendSignedGetAsync<DynamicZhihuResponse>(
                request.Path, 
                request.Params ?? new(), 
                ct);
            return Results.Ok(result);
        })
        .WithSummary("Send a signed request to Zhihu API");
    }
}

public class ZhihuProxyRequest
{
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string>? Params { get; set; }
    public bool EnableSign { get; set; } = true;
}

// ZhihuResponse usually does not have a 'Data' property in the base. 
// It is often Paged or just direct fields. 
// We define a wrapper that adds Data for our proxy standardization.
public class DynamicZhihuResponse : ZhihuResponse
{
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
