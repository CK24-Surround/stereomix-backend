using Edgegap.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edgegap;

public class EdgegapNullClient : IEdgegapClient
{
    private readonly ILogger<EdgegapNullClient> _logger;

    public EdgegapNullClient(ILogger<EdgegapNullClient> logger, IConfiguration configuration)
    {
        _logger = logger;

        Config = new EdgegapConfig
        {
            ApiKey = "EdgegapNullApiKey",
            AppName = "EdgegapNullAppName"
        };
    }

    public EdgegapConfig Config { get; }

    public Task<EdgegapCreateDeploymentResponse> CreateDeploymentAsync(EdgegapCreateDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EdgegapCreateDeploymentResponse
        {
            RequestApp = "EdgegapNullAppName",
            RequestId = "EdgegapNullRequestId",
            RequestDns = "127.0.0.1",
            RequestVersion = "1.0",
            RequestUserCount = 1
        });
    }

    public Task<EdgegapGetDeploymentStatusResponse> GetDeploymentStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EdgegapGetDeploymentStatusResponse
        {
            RequestId = requestId,
            Running = true,
            AppName = "EdgegapNullAppName",
            AppVersion = "1.0",
            Fqdn = "127.0.0.1",
            CurrentStatus = EdgegapDeploymentStatusType.Ready,
            Ports = new Dictionary<string, EdgegapPortMapping>
            {
                ["Game"] = new()
                {
                    External = 7777,
                    Internal = 7777,
                    Link = "127.0.0.1",
                    Name = "Game",
                    Protocol = "UDP",
                    TlsUpgrade = false
                }
            },
            WhitelistingActive = false,
            StartTime = null,
            ElapsedTime = null,
            Error = false
        });
    }

    public Task<EdgegapDeleteDeploymentResponse> DeleteDeploymentAsync(string requestId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EdgegapDeleteDeploymentResponse
        {
            Message = "null"
        });
    }
}
