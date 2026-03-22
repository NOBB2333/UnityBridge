using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UnityBridge.Api.Web.Models.Requests;

namespace UnityBridge.Api.Web.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Auth");

        group.MapPost("/login", Login);
        group.MapPost("/refresh-token", RefreshToken);
        group.MapGet("/get-async-routes", GetAsyncRoutes);
        group.MapGet("/stats", Stats);
    }

    private static IResult Login(LoginRequest input)
    {
        if (input.Username == "admin" && input.Password == "admin123")
        {
            return Results.Ok(new
            {
                success = true,
                data = new
                {
                    avatar = "https://avatars.githubusercontent.com/u/52823142",
                    username = "admin",
                    nickname = "管理员",
                    roles = new[] { "admin" },
                    permissions = new[] { "*:*:*" },
                    accessToken = "eyJhbGciOiJIUzUxMiJ9.admin",
                    refreshToken = "eyJhbGciOiJIUzUxMiJ9.adminRefresh",
                    expires = DateTime.Now.AddHours(24).ToString("yyyy/MM/dd HH:mm:ss")
                }
            });
        }
        return Results.Ok(new { success = false, message = "用户名或密码错误" });
    }

    private static IResult RefreshToken() => Results.Ok(new
    {
        success = true,
        data = new
        {
            accessToken = "eyJhbGciOiJIUzUxMiJ9.adminNew",
            refreshToken = "eyJhbGciOiJIUzUxMiJ9.adminRefreshNew",
            expires = DateTime.Now.AddHours(24).ToString("yyyy/MM/dd HH:mm:ss")
        }
    });

    private static IResult GetAsyncRoutes() => Results.Ok(new { success = true, data = Array.Empty<object>() });

    private static IResult Stats() => Results.Ok(new { message = "stats endpoint" });
}
