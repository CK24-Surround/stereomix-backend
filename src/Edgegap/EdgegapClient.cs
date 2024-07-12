using System.Net;
using System.Net.Http.Json;
using Edgegap.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edgegap;

public interface IEdgegapClient
{
    EdgegapConfig Config { get; }
    Task<EdgegapCreateDeploymentResponse> CreateDeploymentAsync(EdgegapCreateDeploymentRequest request, CancellationToken cancellationToken = default);
    Task<EdgegapGetDeploymentStatusResponse> GetDeploymentStatusAsync(string requestId, CancellationToken cancellationToken = default);
    Task<EdgegapDeleteDeploymentResponse> DeleteDeploymentAsync(string requestId, CancellationToken cancellationToken = default);
}

public class EdgegapClient : IEdgegapClient
{
    private const string ApiKeyEnvironment = "EDGEGAP_API_KEY";

    private const string AppNameEnvironment = "EDGEGAP_APP_NAME";
    // private const string AppVersionEnvironment = "EDGEGAP_APP_VERSION";

    private const string ApiUrl = "https://api.edgegap.com";
    private const string CreateDeploymentResource = "v1/deploy";
    private const string GetDeploymentStatusResource = "v1/status/{request_id}";
    private const string DeleteDeploymentResource = "v1/stop/{request_id}";

    private readonly HttpClient _httpClient;
    private readonly ILogger<EdgegapClient> _logger;

    public EdgegapClient(ILogger<EdgegapClient> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;

        Config = new EdgegapConfig
        {
            ApiKey = configuration[ApiKeyEnvironment] ?? throw new InvalidOperationException($"{ApiKeyEnvironment} is not set"),
            AppName = configuration[AppNameEnvironment] ?? throw new InvalidOperationException($"{AppNameEnvironment} is not set")
            // AppVersion = configuration[AppVersionEnvironment] ?? throw new InvalidOperationException($"{AppVersionEnvironment} is not set")
        };

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(ApiUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", Config.ApiKey);
    }

    public EdgegapConfig Config { get; }

    public async Task<EdgegapCreateDeploymentResponse> CreateDeploymentAsync(EdgegapCreateDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(request, EdgegapJsonSerializerContext.Default.EdgegapCreateDeploymentRequest);
        var response = await _httpClient.PostAsync(CreateDeploymentResource, content, cancellationToken).ConfigureAwait(false);

        var str = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Edgegap CreateDeployment response: {Response}", str);

        if (response.StatusCode is not HttpStatusCode.OK)
        {
            var error = await response.Content.ReadFromJsonAsync(EdgegapJsonSerializerContext.Default.EdgegapErrorResponse, cancellationToken).ConfigureAwait(false);
            _logger.LogError("Deployment creation request failed. Status code: {StatusCode}, message: {Message}", response.StatusCode.ToString(), error!.Message);
            throw new EdgegapException(response.StatusCode, error);
        }

        var responseContent = await response.Content.ReadFromJsonAsync(EdgegapJsonSerializerContext.Default.EdgegapCreateDeploymentResponse, cancellationToken).ConfigureAwait(false);
        return responseContent!;
    }

    public async Task<EdgegapGetDeploymentStatusResponse> GetDeploymentStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(GetDeploymentStatusResource.Replace("{request_id}", requestId), cancellationToken).ConfigureAwait(false);

        var str = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Edgegap GetDeploymentStatus response: {Response}", str);

        if (response.StatusCode is not HttpStatusCode.OK)
        {
            var error = await response.Content.ReadFromJsonAsync(EdgegapJsonSerializerContext.Default.EdgegapErrorResponse, cancellationToken).ConfigureAwait(false);
            _logger.LogError("Get deployment status request failed. Status code: {StatusCode}, message: {Message}", response.StatusCode.ToString(), error!.Message);
            throw new EdgegapException(response.StatusCode, error);
        }

        var responseContent = await response.Content.ReadFromJsonAsync(EdgegapJsonSerializerContext.Default.EdgegapGetDeploymentStatusResponse, cancellationToken).ConfigureAwait(false);
        return responseContent!;
    }

    public async Task<EdgegapDeleteDeploymentResponse> DeleteDeploymentAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(DeleteDeploymentResource.Replace("{request_id}", requestId), cancellationToken).ConfigureAwait(false);

        var str = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Edgegap DeleteDeployment response: {Response}", str);

        if (response.StatusCode is not HttpStatusCode.OK and not HttpStatusCode.Accepted)
        {
            var error = await response.Content.ReadFromJsonAsync(EdgegapJsonSerializerContext.Default.EdgegapErrorResponse, cancellationToken).ConfigureAwait(false);
            _logger.LogError("Delete deployment request failed. Status code: {StatusCode}, message: {Message}", response.StatusCode.ToString(), error!.Message);
            throw new EdgegapException(response.StatusCode, error);
        }

        var responseContent = await response.Content.ReadFromJsonAsync(EdgegapJsonSerializerContext.Default.EdgegapDeleteDeploymentResponse, cancellationToken).ConfigureAwait(false);
        return responseContent!;
    }
}
