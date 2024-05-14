using System.Net;
using System.Text.Json;

using RestSharp;
using RestSharp.Serializers.Json;

using StereoMix.Edgegap.Model;

namespace StereoMix.Edgegap;

public interface IEdgegapService
{
    Task<DeploymentStatus> CreateDeploymentAsync(CreateDeploymentRequest request, CancellationToken cancellationToken = default);
    Task<DeploymentStatus> GetDeploymentStatusAsync(string requestId, CancellationToken cancellationToken = default);
}

public class EdgegapService : IEdgegapService, IDisposable
{
    private const string ApiUrl = "https://api.edgegap.com/v1";
    private readonly RestClient _client;
    private readonly ILogger<EdgegapService> _logger;

    public EdgegapService(ILogger<EdgegapService> logger)
    {
        _logger = logger;

        var authenticator = new EdgegapAuthenticator(Environment.GetEnvironmentVariable("EDGEGAP_API_KEY") ?? throw new InvalidOperationException("EDGEGAP_API_KEY is not set"));
        var options = new RestClientOptions(ApiUrl) { Authenticator = authenticator, ThrowOnAnyError = false };

        _client = new RestClient(options, configureSerialization: s =>
        {
            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.TypeInfoResolverChain.Add(AppJsonSerializerContext.Default);
            s.UseSystemTextJson(jsonSerializerOptions);
        });
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<DeploymentStatus> CreateDeploymentAsync(CreateDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, AppJsonSerializerContext.Default.CreateDeploymentRequest);

            var restRequest = new RestRequest("/deploy", Method.Post);
            restRequest.AddBody(json, ContentType.Json);
            var restResponse = await _client.PostAsync(restRequest, cancellationToken).ConfigureAwait(false);

            if (restResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning("Deployment request failed. Status code: {StatusCode}", restResponse.StatusCode);
                throw new InvalidOperationException("Deployment request failed.");
            }

            _logger.LogInformation("Received response: {Response}", restResponse.Content ?? string.Empty);
            var response = JsonSerializer.Deserialize(restResponse.Content ?? string.Empty, AppJsonSerializerContext.Default.CreateDeploymentResponse) ?? throw new InvalidOperationException("Response is null.");
            DeploymentStatus? status = null;
            var count = 100;
            while (count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException("Task is cancelled by user.");
                }

                status = await GetDeploymentStatusAsync(response.RequestId, cancellationToken).ConfigureAwait(false);
                if (status.Running)
                {
                    break;
                }

                _logger.LogDebug("Request Id [{RequestId}] Deployment status: {Status}, Remaining retry count: {count}", status.RequestId, status.CurrentStatus, count);
                count--;
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }

            if (status is null || !status.Running)
            {
                _logger.LogWarning("Request Id [{RequestId}] Deployment failed.", response.RequestId);
                throw new InvalidOperationException("Deployment failed.");
            }

            _logger.LogInformation("Request Id [{RequestId}] Deployment complete.", status.RequestId);
            return status;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError(e, "Unauthorized request to Edgegap API.");
            throw new InvalidOperationException("Unauthorized request to Edgegap API.");
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Error while creating deployment. {Message}", e.Message);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while creating deployment.");
            throw;
        }
    }

    public async Task<DeploymentStatus> GetDeploymentStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var restRequest = new RestRequest($"/status/{requestId}");
            var restResponse = await _client.GetAsync(restRequest, cancellationToken).ConfigureAwait(false);

            if (restResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning("Deployment status request failed. Status code: {StatusCode}", restResponse.StatusCode);
                throw new InvalidOperationException("Deployment status request failed.");
            }

            _logger.LogInformation("Received response: {Response}", restResponse.Content ?? string.Empty);
            var response = JsonSerializer.Deserialize(restResponse.Content ?? string.Empty, AppJsonSerializerContext.Default.DeploymentStatus);

            if (response is null)
            {
                throw new InvalidOperationException("Response is null.");
            }

            return response;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError(e, "Unauthorized request to Edgegap API.");
            throw new InvalidOperationException("Unauthorized request to Edgegap API.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting deployment status.");
            throw;
        }
    }
}
