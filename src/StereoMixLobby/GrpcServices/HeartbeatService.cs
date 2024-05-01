using Grpc.Core;
using Microsoft.AspNetCore.WebUtilities;

namespace StereoMixLobby.GrpcServices;

public class HeartbeatService(ILogger<HeartbeatService> logger) : Heartbeat.HeartbeatBase
{
    public override async Task Monitor(IAsyncStreamReader<Beat> requestStream, IServerStreamWriter<MonitorResponse> responseStream, ServerCallContext context)
    {
        try
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                logger.LogInformation("Received heartbeat from {Id} at {Timestamp}", request.Id, request.Timestamp);
                await responseStream.WriteAsync(new MonitorResponse { Ok = true }).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning("Error reading heartbeat: {Message}", e.Message);
        }
    }
}
