using Microsoft.AspNetCore.Mvc;
using UnityBridge.Crawler.BiliBili;
using System.Text.Json.Serialization;

namespace UnityBridge.Api.Sdk.Endpoints;

public static class BiliBiliEndpoints
{
    public static void MapBiliBiliEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bilibili").WithTags("BiliBili");

        group.MapPost("/request", async (
            [FromServices] BiliClient client, 
            [FromBody] BiliProxyRequest request,
            CancellationToken ct) =>
        {
            // BiliClient expects parameters to be Strings
            var result = await client.SendSignedGetAsync<DynamicBiliResponse>(
                request.Path, 
                request.Params ?? new(), 
                request.EnableSign, 
                ct);
            return Results.Ok(result);
        })
        .WithSummary("Send a signed request to BiliBili API")
        .WithDescription("Proxies a request to BiliBili with automatic signing (WBI).");
    }
}

public class BiliProxyRequest
{
    /// <summary>
    /// The API path (e.g. "https://api.bilibili.com/x/space/wbi/arc/search")
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Query parameters
    /// </summary>
    public Dictionary<string, string>? Params { get; set; }

    /// <summary>
    /// Whether to apply WBI signature
    /// </summary>
    public bool EnableSign { get; set; } = true;
}

/// <summary>
/// Dynamic response wrapper for arbitrary JSON data
/// </summary>
public class DynamicBiliResponse : BiliResponse<object> { }
