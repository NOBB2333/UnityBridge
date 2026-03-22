using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using UnityBridge.Api.Web;
using UnityBridge.Api.Web.Endpoints;
using UnityBridge.Api.Web.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args); // AOT optimized builder

// Configure JSON Serializer for AOT
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = null; // Ensure PascalCase
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// AOT compatible Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Services
var repoPath = builder.Configuration["Git:RepoPath"] ?? Path.Combine(AppContext.BaseDirectory, "dcm-repo");
var dbPath = builder.Configuration["Database:Path"] ?? Path.Combine(AppContext.BaseDirectory, "dcm.db");
builder.Services.AddSingleton(sp => new GitService(repoPath));
builder.Services.AddSingleton(sp => new DbService(dbPath));

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// 初始化种子数据 (生产环境可注释掉这行)
var dbService = app.Services.GetRequiredService<DbService>();
var gitService = app.Services.GetRequiredService<GitService>();
SeedService.SeedData(dbService, gitService);

// OpenAPI/Swagger UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // AOT compatible UI replacement for SwaggerUI
}

app.UseCors();

// Map Minimal APIs
app.MapTenantsEndpoints();
app.MapFilesEndpoints();
app.MapGitEndpoints();
app.MapAuthEndpoints();

// Static Files & SPA Fallback (Embedded Resources)
var assembly = Assembly.GetExecutingAssembly();
// 默认尝试以 root 方式加载 (最通用)
var embeddedProvider = new ManifestEmbeddedFileProvider(assembly);

// 检查 wwwroot 是否在子目录下 (因为 LinkBase="wwwroot")
if (!embeddedProvider.GetFileInfo("index.html").Exists && 
    embeddedProvider.GetFileInfo("wwwroot/index.html").Exists)
{
    // 如果文件在 wwwroot/index.html，则使用 "wwwroot" 作为根重新初始化
    embeddedProvider = new ManifestEmbeddedFileProvider(assembly, "wwwroot");
}
IFileProvider fileProvider = embeddedProvider;

if (Directory.Exists(Path.Combine(AppContext.BaseDirectory, "wwwroot")))
{
    var physicalProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "wwwroot"));
    fileProvider = new CompositeFileProvider(physicalProvider, embeddedProvider);
    Console.WriteLine("Static files: Physical + Embedded");
}
else
{
    Console.WriteLine("Static files: Embedded resources only");
}

app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });

app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = 404;
        return;
    }
    
    var indexFile = fileProvider.GetFileInfo("index.html");
    if (indexFile.Exists)
    {
        context.Response.ContentType = "text/html";
        await using var stream = indexFile.CreateReadStream();
        await stream.CopyToAsync(context.Response.Body);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

Console.WriteLine($"Git repo: {repoPath}");
Console.WriteLine($"Database: {dbPath}");

app.Run();
