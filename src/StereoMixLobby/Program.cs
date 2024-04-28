using Microsoft.AspNetCore.Server.Kestrel.Core;
using StereoMixLobby.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.MapGet("/", () => "StereoMix Lobby Service");
app.MapGrpcService<GreeterService>();

app.Run();
