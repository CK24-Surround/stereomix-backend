using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using StereoMix.Auth;
using StereoMix.Firestore;
using StereoMix.JWT;

namespace StereoMix.Grpc;

public class AuthService(
    ILogger<AuthService> logger,
    IJwtTokenService jwtToken,
    IFirestoreService firestore) : Auth.AuthService.AuthServiceBase
{
    [AllowAnonymous]
    public override Task<Response> GuestLogin(GuestLoginRequest request, ServerCallContext context)
    {
        try
        {
            var token = jwtToken.AuthenticateTemporary(request.UserName);
            logger.LogInformation("New token generated for user {User}: {Token}", request.UserName, token);
            return Task.FromResult(new Response { Token = token });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Internal error while processing login.");
            throw new RpcException(new Status(StatusCode.Internal, "Internal error while processing login."));
        }
    }
}
