using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace StereoMix.Hathora;

public interface IHathoraCloudService
{
    Task<HathoraGetConnectionInfoResponse> CreateRoomAsync(HathoraCreateRoomRequest request, CancellationToken cancellationToken = default);
    Task<HathoraGetConnectionInfoResponse> GetConnectionInfoAsync(HathoraGetConnectionInfoRequest request);
}

public class HathoraAuthenticator(string apiKey) : IAuthenticator
{
    public ValueTask Authenticate(IRestClient client, RestRequest request)
    {
        request.AddHeader("Authorization", apiKey);
        return ValueTask.CompletedTask;
    }
}

public class HathoraCloudService : IHathoraCloudService, IDisposable
{
    private const string ApiUrl = "https://api.hathora.dev";
    private readonly string _appId;
    private readonly RestClient _client;
    private readonly ILogger<HathoraCloudService> _logger;

    public HathoraCloudService(ILogger<HathoraCloudService> logger)
    {
        _logger = logger;
        _appId = Environment.GetEnvironmentVariable("HATHORA_APP_ID") ?? throw new InvalidOperationException("HATHORA_APP_ID is not set");

        var token = Environment.GetEnvironmentVariable("HATHORA_API_TOKEN") ?? throw new InvalidOperationException("HATHORA_API_TOKEN is not set");
        var authenticator = new JwtAuthenticator(token);
        var options = new RestClientOptions(ApiUrl) { Authenticator = authenticator };

        _client = new RestClient(options, configureSerialization: s =>
        {
            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.TypeInfoResolverChain.Add(AppJsonSerializerContext.Default);
            s.UseSystemTextJson(jsonSerializerOptions);
        });
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<HathoraGetConnectionInfoResponse> CreateRoomAsync(HathoraCreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var resource = $"/rooms/v2/{_appId}/create";

        var response = await RequestAsync(resource, Method.Post, request,
            AppJsonSerializerContext.Default.HathoraCreateRoomRequest,
            AppJsonSerializerContext.Default.HathoraCreateRoomResponse,
            cancellationToken).ConfigureAwait(false);

        var getConnectionInfoRequest = new HathoraGetConnectionInfoRequest
        {
            AppId = _appId,
            RoomId = response.RoomId
        };
        var count = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            var connectionInfo = await GetConnectionInfoAsync(getConnectionInfoRequest).ConfigureAwait(false);
            _logger.LogDebug("RoomReadyStatus: {Status}", connectionInfo.Status);
            if (connectionInfo.Status == HathoraRoomReadyStatus.Active)
            {
                return connectionInfo;
            }

            count++;
            if (count >= 60)
            {
                break;
            }
        }

        throw new HttpRequestException(HttpRequestError.InvalidResponse, "CreateRoom timed out.");
    }

    public async Task<HathoraGetConnectionInfoResponse> GetConnectionInfoAsync(HathoraGetConnectionInfoRequest request)
    {
        var resource = $"/rooms/v2/{_appId}/connectioninfo/{request.RoomId}";
        return await RequestAsync(resource, Method.Get,
            AppJsonSerializerContext.Default.HathoraGetConnectionInfoResponse).ConfigureAwait(false);
    }

    private async Task<TResponse> RequestAsync<TResponse>(
        string resource, Method method,
        JsonTypeInfo<TResponse> responseJsonTypeInfo,
        CancellationToken cancellationToken = default)
        where TResponse : HathoraResponse
    {
        var restRequest = new RestRequest(resource, method);
        return await RequestAsync(restRequest, responseJsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> RequestAsync<TRequest, TResponse>(
        string resource, Method method, TRequest request,
        JsonTypeInfo<TRequest> requestJsonTypeInfo, JsonTypeInfo<TResponse> responseJsonTypeInfo,
        CancellationToken cancellationToken = default)
        where TRequest : HathoraRequest
        where TResponse : HathoraResponse
    {
        var jsonBody = JsonSerializer.Serialize(request, requestJsonTypeInfo);
        _logger.LogDebug("Request: {Resource} {JsonBody}", resource, jsonBody);

        var restRequest = new RestRequest(resource, method);
        restRequest.AddJsonBody(jsonBody);
        return await RequestAsync(restRequest, responseJsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> RequestAsync<TResponse>(
        RestRequest request,
        JsonTypeInfo<TResponse> responseJsonTypeInfo,
        CancellationToken cancellationToken = default)
        where TResponse : HathoraResponse
    {
        RestResponse restResponse;
        try
        {
            restResponse = await _client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Request [{Resource}] failed ({StatusCode}): {Message}", request.Resource, e.StatusCode, e.Message);
            throw;
        }

        var responseContent = restResponse.Content ?? throw new HttpRequestException("Response content is null.");
        _logger.LogDebug("Response [{Resource}]: {Response}", request.Resource, responseContent);

        TResponse? response;
        try
        {
            response = JsonSerializer.Deserialize(responseContent, responseJsonTypeInfo);
        }
        catch (JsonSerializationException e)
        {
            _logger.LogError(e, "Failed to deserialize response for [{Resource}].", request.Resource);
            throw;
        }

        if (restResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            return response ?? throw new HttpRequestException(HttpRequestError.InvalidResponse, "Response is null.");
        }

        _logger.LogWarning("[{Resource}] request failed. Status code: {StatusCode}", request.Resource, restResponse.StatusCode);
        throw new HttpRequestException(
            HttpRequestError.InvalidResponse,
            response?.ErrorMessage ?? restResponse.ErrorMessage,
            statusCode: restResponse.StatusCode);
    }
}
