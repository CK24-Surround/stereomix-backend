using Grpc.Core;
using Grpc.Net.Client;
using StereoMix.Auth;
using StereoMix.Greet;

if (args.Length < 1)
{
    Console.WriteLine("Pass address as argument.");
    return;
}

var endPoint = "dns:///" + args[0];

Console.WriteLine("Connecting to " + endPoint + "...");

var credentials = endPoint.Contains("localhost") ? ChannelCredentials.Insecure : ChannelCredentials.SecureSsl;

var channel = GrpcChannel.ForAddress(endPoint, new GrpcChannelOptions { Credentials = credentials });
await channel.ConnectAsync().ConfigureAwait(false);
Console.WriteLine("Connected.");

var authService = new AuthService.AuthServiceClient(channel);
Console.WriteLine("Requesting token...");
var loginResponse = await authService.GuestLoginAsync(new GuestLoginRequest { UserName = "TestUser" });
Console.WriteLine("Token: " + loginResponse.AccessToken);

var headers = new Metadata { { "authorization", "Bearer " + loginResponse.AccessToken } };

var greeter = new GreeterService.GreeterServiceClient(channel);
try
{
    var reply = await greeter.SayHelloAsync(new HelloRequest { Name = "StereoMixTestClient" }, headers);
    Console.WriteLine(reply.Message);
}
catch (RpcException e) when (e.StatusCode == StatusCode.Unauthenticated)
{
    Console.WriteLine($"Unauthenticated. {e.Message}");
}
