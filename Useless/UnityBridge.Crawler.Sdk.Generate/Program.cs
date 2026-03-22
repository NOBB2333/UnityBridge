using Scalar.AspNetCore;
using UnityBridge.Api.Sdk.Endpoints;
using UnityBridge.Api.Sdk.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSdkServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapBiliBiliEndpoints();
app.MapDouyinEndpoints();
app.MapKuaishouEndpoints();
app.MapTiebaEndpoints();
app.MapWeiboEndpoints();
app.MapXhsEndpoints();
app.MapZhihuEndpoints();

app.Run();
