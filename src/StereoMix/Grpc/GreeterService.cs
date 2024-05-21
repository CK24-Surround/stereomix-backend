using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using StereoMix.Greet;

namespace StereoMix.Grpc;

public class GreeterService(ILogger<GreeterService> logger) : Greet.GreeterService.GreeterServiceBase
{
    [Authorize]
    public override Task<HelloResponse> SayHello(HelloRequest request, ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        logger.LogInformation("User {User} is calling GreeterServiceV1.SayHello", user.Identity?.Name ?? "NULL");

        return Task.FromResult(new HelloResponse { Message = $"Hello {request.Name}" });
    }
}
