using Microsoft.AspNetCore.Mvc;
using UnityBridge.Crawler.XiaoHongShu;
using System.Text.Json.Serialization;
using System.Linq;

namespace UnityBridge.Api.Sdk.Endpoints;

public static class XhsEndpoints
{
    public static void MapXhsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/xhs").WithTags("XiaoHongShu");

        group.MapPost("/request", async (
            [FromServices] XhsClient client, 
            [FromBody] XhsProxyRequest request,
            CancellationToken ct) =>
        {
            var path = request.Path;
            if (request.Params != null && request.Params.Count > 0)
            {
                var queryString = string.Join("&", request.Params.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                path += "?" + queryString;
            }

            // Use our concrete ProxyXhsRequest
            var xhsReq = new ProxyXhsRequest();

            var result = await client.SendSignedGetAsync<DynamicXhsResponse>(
                xhsReq, 
                ct, 
                path);
            return Results.Ok(result);
        })
        .WithSummary("Send a signed request to XiaoHongShu API");
    }
}

public class XhsProxyRequest
{
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string>? Params { get; set; }
    public bool EnableSign { get; set; } = true;
}

// Concrete implementation of abstract XhsRequest
public class ProxyXhsRequest : XhsRequest
{
}

public class DynamicXhsResponse : XhsResponse
{
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
