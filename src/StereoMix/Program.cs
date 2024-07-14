using System.Net;
using System.Text;
using Edgegap;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using StereoMix;
using StereoMix.Discord;
using StereoMix.Firestore;
using StereoMix.Grpc;
using StereoMix.Security;
using StereoMix.Storage;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

builder.WebHost.UseKestrel(options =>
{
    var port = int.Parse(configuration["PORT"] ?? "8080");
    options.Listen(IPAddress.Any, port, listenOptions => { listenOptions.Protocols = HttpProtocols.Http2; });
});

services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var key = configuration["JWT_SECRET"] ?? throw new KeyNotFoundException("JWT_SECRET is not set in Environment Variables");
        var issuer = configuration["StereoMix:JWT:Issuer"] ?? throw new KeyNotFoundException("StereoMix:JWT:Issuer is not set in appsettings.json");
        var gameServerAudience = configuration["StereoMix:JWT:Audience:GameServer"] ?? throw new KeyNotFoundException("StereoMix:JWT:Audience:GameServer is not set in appsettings.json");
        var clientAudience = configuration["StereoMix:JWT:Audience:Client"] ?? throw new KeyNotFoundException("StereoMix:JWT:Audience:Client is not set in appsettings.json");

        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudiences = [gameServerAudience, clientAudience],
            ValidateLifetime = true
        };
    });

services
    .AddAuthorizationBuilder()
    .AddPolicy(StereoMixPolicy.AuthorizeUserOnlyPolicy, policy =>
    {
        policy.RequireRole(StereoMixRole.UserRole);
        // policy.RequireClaim(StereoMixClaimTypes.Role, StereoMixRole.UserRole);
        policy.RequireClaim(StereoMixClaimTypes.UserId);
        policy.RequireClaim(StereoMixClaimTypes.UserName);
    })
    .AddPolicy(StereoMixPolicy.AuthorizeGameServerOnlyPolicy, policy =>
    {
        policy.RequireRole(StereoMixRole.GameServerRole);
        // policy.RequireClaim(StereoMixClaimTypes.Role, StereoMixRole.GameServerRole);
        policy.RequireClaim(StereoMixClaimTypes.RoomId);
    });

services.AddGrpc();
services.AddGrpcHealthChecks().AddCheck("StereoMix", () => HealthCheckResult.Healthy());
services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
services.AddSingleton<IFirestoreClient, FirestoreClient>();
services.AddSingleton<IRoomEncryptor, RoomEncryptor>();
services.AddSingleton<IUserStorage, UserStorage>();
services.AddSingleton<ILobbyStorage, LobbyStorage>();
services.AddSingleton<DiscordMatchNotify>();

// services.AddHttpClient<IEdgegapClient, EdgegapClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
if (configuration["ASPNETCORE_ENVIRONMENT"] == "Production")
{
    services.AddHttpClient<IEdgegapClient, EdgegapClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
}
else
{
    services.AddSingleton<IEdgegapClient, EdgegapNullClient>();
}

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcHealthChecksService();
app.MapGrpcService<GameService>();
app.MapGrpcService<AuthService>();
app.MapGrpcService<GreeterService>();
app.MapGrpcService<LobbyService>();
app.Run();
