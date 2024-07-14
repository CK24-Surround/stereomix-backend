using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using StereoMix.Game;

namespace StereoMix.Grpc;

public class GameService : Game.GameService.GameServiceBase
{
    private const string ServiceVersionEnvironment = "STEREOMIX_SERVICE_VERSION";

    protected readonly ILogger<GameService> Logger;

    protected readonly string ServiceVersion;

    public GameService(ILogger<GameService> logger, IConfiguration configuration)
    {
        Logger = logger;
        ServiceVersion = configuration[ServiceVersionEnvironment] ?? throw new InvalidOperationException($"{ServiceVersionEnvironment} is not set");
        logger.LogInformation("StereoMix GameService {ServiceVersion} is starting", ServiceVersion);
    }

    [AllowAnonymous]
    public override Task<GetServiceVersionResponse> GetServiceVersion(GetServiceVersionRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetServiceVersionResponse { Version = ServiceVersion });
    }
}
