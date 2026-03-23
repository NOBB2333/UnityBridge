using Microsoft.AspNetCore.Mvc;
using UnityBridge.Crawler.Tieba;
using System.Text.Json.Serialization;

namespace UnityBridge.Api.Sdk.Endpoints;

public static class TiebaEndpoints
{
    public static void MapTiebaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tieba").WithTags("Tieba");

        group.MapPost("/request", async (
            [FromServices] TiebaClient client, 
            [FromBody] TiebaProxyRequest request,
            CancellationToken ct) =>
        {
            var result = await client.SendGetJsonAsync<DynamicTiebaResponse>(
                request.Path, 
                request.Params, 
                ct);
            return Results.Ok(result);
        })
        .WithSummary("Send a signed request to Tieba API");
    }
}

public class TiebaProxyRequest
{
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string>? Params { get; set; }
    public bool EnableSign { get; set; } = true;
}

public class DynamicTiebaResponse : TiebaResponse<object> { }
