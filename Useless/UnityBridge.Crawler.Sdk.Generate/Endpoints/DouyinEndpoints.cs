using Microsoft.AspNetCore.Mvc;
using UnityBridge.Crawler.Douyin;
using System.Text.Json.Serialization;

namespace UnityBridge.Api.Sdk.Endpoints;

public static class DouyinEndpoints
{
    public static void MapDouyinEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/douyin").WithTags("Douyin");

        group.MapPost("/request", async (
            [FromServices] DouyinClient client, 
            [FromBody] DouyinProxyRequest request,
            CancellationToken ct) =>
        {
            var result = await client.SendSignedGetAsync<DynamicDouyinResponse>(
                request.Path, 
                request.Params ?? new(), 
                request.EnableSign, 
                ct);
            return Results.Ok(result);
        })
        .WithSummary("Send a signed request to Douyin API")
        .WithDescription("Proxies a request to Douyin with automatic signing (a_bogus).");
    }
}

public class DouyinProxyRequest
{
    /// <summary>
    /// The API path
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Query parameters
    /// </summary>
    public Dictionary<string, string>? Params { get; set; }

    /// <summary>
    /// Whether to apply signature
    /// </summary>
    public bool EnableSign { get; set; } = true;
}

/// <summary>
/// Dynamic response wrapper for arbitrary JSON data
/// </summary>
public class DynamicDouyinResponse : DouyinResponse<object> { }
