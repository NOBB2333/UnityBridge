using Microsoft.AspNetCore.Mvc;
using UnityBridge.Crawler.Weibo;
using System.Text.Json.Serialization;

namespace UnityBridge.Api.Sdk.Endpoints;

public static class WeiboEndpoints
{
    public static void MapWeiboEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weibo").WithTags("Weibo");

        group.MapPost("/request", async (
            [FromServices] WeiboClient client, 
            [FromBody] WeiboProxyRequest request,
            CancellationToken ct) =>
        {
            var result = await client.SendGetJsonAsync<DynamicWeiboResponse>(
                request.Path, 
                request.Params, 
                null, 
                ct);
            return Results.Ok(result);
        })
        .WithSummary("Send a signed request to Weibo API");
    }
}

public class WeiboProxyRequest
{
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string>? Params { get; set; }
    public bool EnableSign { get; set; } = true;
}

public class DynamicWeiboResponse : WeiboResponse<object> { }
