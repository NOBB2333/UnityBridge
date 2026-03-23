using Microsoft.AspNetCore.Mvc;
using UnityBridge.Crawler.Kuaishou;
using System.Text.Json.Serialization;

namespace UnityBridge.Api.Sdk.Endpoints;

public static class KuaishouEndpoints
{
    public static void MapKuaishouEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/kuaishou").WithTags("Kuaishou");

        group.MapPost("/request", async (
            [FromServices] KuaishouClient client, 
            [FromBody] KuaishouGraphQLRequest request,
            CancellationToken ct) =>
        {
            var result = await client.SendGraphQLAsync<DynamicKuaishouResponse>(
                request.OperationName, 
                request.Variables ?? new object(), 
                request.Query, 
                ct);
            return Results.Ok(result);
        })
        .WithSummary("Send a GraphQL request to Kuaishou API");
    }
}

public class KuaishouGraphQLRequest
{
    public string OperationName { get; set; } = string.Empty;
    public object? Variables { get; set; }
    public string Query { get; set; } = string.Empty;
}

public class DynamicKuaishouResponse : KuaishouResponse<object> { }
