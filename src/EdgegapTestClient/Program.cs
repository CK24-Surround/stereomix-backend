// See https://aka.ms/new-console-template for more information

using Edgegap;
using Edgegap.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

Console.WriteLine("Hello, World!");

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var handler = new HttpClientHandler { AllowAutoRedirect = false };
using var httpClient = new HttpClient(handler);
var edgegap = new EdgegapClient(NullLogger<EdgegapClient>.Instance, httpClient, configuration);

var request = new EdgegapCreateDeploymentRequest
{
    AppName = "stereomix",
    VersionName = "demo-v1.1",
    IpList = ["180.224.212.10"],
    Tags = [],
    EdgegapApSortStrategyType = EdgegapApSortStrategyType.Weighted,
    EnvVars =
    [
        new EdgegapDeployEnvironment
        {
            Key = "STEREOMIX_AUTH_TOKEN",
            Value = "abscsdfcsdfkj314n1jk3123",
            IsHidden = true
        }
    ]
};

var response = await edgegap.CreateDeploymentAsync(request).ConfigureAwait(false);
Console.WriteLine(response.RequestId);
