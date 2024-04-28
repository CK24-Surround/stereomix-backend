// See https://aka.ms/new-console-template for more information

using Grpc.Net.Client;
using StereoMixLobby;

Console.WriteLine("Hello, World!");

if (args.Length < 1)
{
    Console.WriteLine("Pass address as argument.");
    return;
}

var channel = GrpcChannel.ForAddress(args[0]);

var greeter = new Greeter.GreeterClient(channel);

var reply = await greeter.SayHelloAsync(new HelloRequest { Name = "StereoMixTestClient" });
Console.WriteLine(reply.Message);
