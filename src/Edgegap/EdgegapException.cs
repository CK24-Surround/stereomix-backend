using System.Net;
using Edgegap.Model;

namespace Edgegap;

public class EdgegapException(HttpStatusCode statusCode, EdgegapErrorResponse response) : Exception(response.Message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public EdgegapErrorResponse Response { get; } = response;

    public override string ToString()
    {
        return $"StatusCode: {StatusCode}, Message: {Message}";
    }
}
