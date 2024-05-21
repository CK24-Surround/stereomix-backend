using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using StereoMix.Auth;
using StereoMix.Security;
using StereoMix.Storage;

namespace StereoMix.Grpc;

public class AuthService(
    ILogger<AuthService> logger,
    IUserStorage userStorage,
    IJwtTokenProvider jwtTokenProvider) : Auth.AuthService.AuthServiceBase
{
    [AllowAnonymous]
    public override async Task<LoginResponse> GuestLogin(GuestLoginRequest request, ServerCallContext context)
    {
        var userName = request.UserName;
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "User name cannot be empty."));
        }

        var newUserData = new UserStorageData
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = userName
        };
        var response = await userStorage.CreateUserAccountAsync(newUserData, context.CancellationToken).ConfigureAwait(false);

        if (response is not StorageResponse.Success)
        {
            logger.LogError("Failed to create user account. {Response}", response);
            throw new RpcException(new Status(StatusCode.Internal, "Internal error while creating user account."));
        }

        var token = jwtTokenProvider.AuthenticateUser(newUserData.Id, newUserData.Name);
        logger.LogDebug("New token generated for user {UserId}", newUserData.Id);
        return new LoginResponse { Token = token };
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeGameServerOnlyPolicy)]
    public override async Task<ValidateUserTokenResponse> ValidateUserToken(ValidateUserTokenRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Token cannot be empty."));
        }

        var validateResult = await jwtTokenProvider.ValidateTokenAsync(request.Token).ConfigureAwait(false);
        if (validateResult.IsValid)
        {
            var userIdClaim = validateResult.ClaimsIdentity.FindFirst(StereoMixClaimTypes.UserId);
            var userNameClaim = validateResult.ClaimsIdentity.FindFirst(StereoMixClaimTypes.UserName);
            if (userIdClaim is null || userNameClaim is null)
            {
                logger.LogDebug("Token validation passed but it does not contain user information. {Token}", request.Token);
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Token does not contain user information."));
            }

            logger.LogDebug("Token validation passed. {Token}", request.Token);
            return new ValidateUserTokenResponse
            {
                IsValid = true,
                UserId = userIdClaim.Value,
                UserName = userNameClaim.Value
            };
        }

        logger.LogDebug("Token validation failed. {Token}, Exception: {Exception}", request.Token, validateResult.Exception.Message);
        return new ValidateUserTokenResponse { IsValid = false };
    }
}
