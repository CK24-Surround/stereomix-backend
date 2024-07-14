using Edgegap.Model;

namespace Edgegap;

public interface IEdgegapClient
{
    EdgegapConfig Config { get; }
    Task<EdgegapCreateDeploymentResponse> CreateDeploymentAsync(EdgegapCreateDeploymentRequest request, CancellationToken cancellationToken = default);
    Task<EdgegapGetDeploymentStatusResponse> GetDeploymentStatusAsync(string requestId, CancellationToken cancellationToken = default);
    Task<EdgegapDeleteDeploymentResponse> DeleteDeploymentAsync(string requestId, CancellationToken cancellationToken = default);
}
