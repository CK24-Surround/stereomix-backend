using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StereoMixLobby.GrpcServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddGrpc();

builder.WebHost.ConfigureKestrel((_, options) =>
{
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        listenOptions.UseHttps();
    });
});

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "StereoMix Lobby Service");
app.MapGrpcService<GreeterService>();
app.MapGrpcService<HeartbeatService>();

app.Run();
