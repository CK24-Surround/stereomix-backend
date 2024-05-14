using RestSharp;
using RestSharp.Authenticators;

namespace StereoMix.Edgegap;

public class EdgegapAuthenticator(string apiKey) : IAuthenticator
{
    public ValueTask Authenticate(IRestClient client, RestRequest request)
    {
        request.AddHeader("Authorization", apiKey);
        return ValueTask.CompletedTask;
    }
}
