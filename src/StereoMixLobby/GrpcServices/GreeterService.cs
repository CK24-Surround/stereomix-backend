using Grpc.Core;

namespace StereoMixLobby.GrpcServices;

public class GreeterService(ILogger<GreeterService> logger) : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}" });
    }
}
