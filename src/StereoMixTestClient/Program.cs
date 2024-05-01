using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using StereoMixLobby;

Console.WriteLine("Hello, World!");

if (args.Length < 1)
{
    Console.WriteLine("Pass address as argument.");
    return;
}

var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

var channel = GrpcChannel.ForAddress(args[0], new GrpcChannelOptions { HttpHandler = handler });
var greeter = new Greeter.GreeterClient(channel);

var reply = await greeter.SayHelloAsync(new HelloRequest { Name = "StereoMixTestClient" });

Console.WriteLine(reply.Message);

await HeartbeatAsync(channel).ConfigureAwait(false);
return;

static async Task HeartbeatAsync(GrpcChannel channel)
{
    var heartbeatClient = new Heartbeat.HeartbeatClient(channel);
    using var heartbeatStream = heartbeatClient.Monitor();

    var cts = new CancellationTokenSource();

    Console.WriteLine("Starting background task to receive messages.");
    var readTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var response in heartbeatStream.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine(response.Ok);
            }
        }
        catch (RpcException e)
        {
            Console.WriteLine($"Rpc error: {e.Status.StatusCode}");
        }
        catch (TaskCanceledException)
        {
        }
    });

    var writeTask = Task.Run(async () =>
    {
        while (!cts.Token.IsCancellationRequested)
        {
            await heartbeatStream.RequestStream.WriteAsync(new Beat
            {
                Id = "StereoMixTestClient", Timestamp = DateTime.Now.ToString("O")
            }).ConfigureAwait(false);
            if (cts.IsCancellationRequested)
            {
                return;
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }
    });

    Console.ReadKey();
    cts.Cancel();

    await heartbeatStream.RequestStream.CompleteAsync().ConfigureAwait(false);
    await Task.WhenAll(readTask, writeTask).ConfigureAwait(false);
    Console.WriteLine("Task done.");
}
