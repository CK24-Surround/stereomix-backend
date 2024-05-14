using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using StereoMix;
using StereoMix.Edgegap;
using StereoMix.Firestore;
using StereoMix.Grpc;
using StereoMix.Hathora;
using StereoMix.JWT;
using StereoMix.Security;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost
    .UseKestrel(options =>
    {
        var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "8080");
        options.Listen(IPAddress.Any, port, listenOptions => { listenOptions.Protocols = HttpProtocols.Http2; });
    });

var configuration = builder.Configuration;

configuration.AddEnvironmentVariables("JWT_");

var services = builder.Services;

services.ConfigureHttpJsonOptions(options => { options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default); });

// TODO: Configure로 JWT 환경변수 옮기기
services.Configure<JwtConfiguration>(options =>
{
    options.Secret = configuration["JWT_SECRET"] ?? throw new KeyNotFoundException("Can not found JWT_SECRET");
    options.Issuer = configuration["JWT_ISSUER"] ?? throw new KeyNotFoundException("Can not found JWT_ISSUER");
    options.Audience = configuration["JWT_AUDIENCE"] ?? throw new KeyNotFoundException("Can not found JWT_AUDIENCE");
});

services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var key = Environment.GetEnvironmentVariable(JwtTokenService.JwtSecretKeyName) ?? throw new InvalidOperationException("JWT_SECRET is not set");
        var keyBytes = Encoding.ASCII.GetBytes(key);
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new InvalidOperationException("JWT_ISSUER is not set"),
            ValidateAudience = true,
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new InvalidOperationException("JWT_AUDIENCE is not set"),
            ValidateLifetime = true
        };
    });

services
    .AddAuthorizationBuilder()
    .AddPolicy("UserPolicy", policy =>
        policy
            .RequireAuthenticatedUser()
            .RequireRole("User")
            .RequireClaim(ClaimTypes.NameIdentifier)
            .RequireClaim(ClaimTypes.Name));

services.AddGrpc(options => { options.EnableDetailedErrors = true; });
services.AddGrpcHealthChecks().AddCheck("StereoMix", () => HealthCheckResult.Healthy());

services.AddSingleton<IJwtTokenService, JwtTokenService>();
services.AddSingleton<IFirestoreService, FirestoreService>();
services.AddSingleton<IRoomEncryptService, RoomEncryptService>();
services.AddSingleton<IEdgegapService, EdgegapService>();
services.AddSingleton<IHathoraCloudService, HathoraCloudService>();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcHealthChecksService();

// v1
app.MapGrpcService<AuthService>();
// app.MapGrpcService<GreeterService>();
app.MapGrpcService<LobbyService>();

app.Run();
