using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using StereoMix.Auth;
using StereoMix.Security;
using StereoMix.Storage;

namespace StereoMix.Grpc;

public class AuthService : Auth.AuthService.AuthServiceBase
{
    protected readonly IJwtTokenProvider JwtTokenProvider;
    protected readonly ILogger<AuthService> Logger;
    protected readonly IUserStorage UserStorage;

    public AuthService(ILogger<AuthService> logger, IUserStorage userStorage, IJwtTokenProvider jwtTokenProvider)
    {
        Logger = logger;
        UserStorage = userStorage;
        JwtTokenProvider = jwtTokenProvider;
    }

    [AllowAnonymous]
    public override async Task<LoginResponse> GuestLogin(GuestLoginRequest request, ServerCallContext context)
    {
        var userName = request.UserName;
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "User name cannot be empty."));
        }

        var userAccount = new UserAccount
        {
            UserId = Guid.NewGuid().ToString("N"),
            UserName = userName
        };

        var response = await UserStorage.CreateUserAccountAsync(new UserStorageData
        {
            Id = userAccount.UserId,
            Name = userAccount.UserName
        }, context.CancellationToken).ConfigureAwait(false);

        if (response is not StorageResponse.Success)
        {
            Logger.LogError("Failed to create user account. {Response}", response);
            throw new RpcException(new Status(StatusCode.Internal, "Internal error while creating user account."));
        }

        var token = JwtTokenProvider.AuthenticateUser(userAccount);
        Logger.LogDebug("New access token generated for user {UserName}({UserId})", userName, userAccount.UserId);
        return new LoginResponse { AccessToken = token, UserAccount = userAccount };
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeGameServerOnlyPolicy)]
    public override async Task<ValidateUserTokenResponse> ValidateUserToken(ValidateUserTokenRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Token cannot be empty."));
        }

        var validateResult = await JwtTokenProvider.ValidateTokenAsync(request.AccessToken).ConfigureAwait(false);
        if (validateResult.IsValid)
        {
            var userIdClaim = validateResult.ClaimsIdentity.FindFirst(StereoMixClaimTypes.UserId);
            var userNameClaim = validateResult.ClaimsIdentity.FindFirst(StereoMixClaimTypes.UserName);
            if (userIdClaim is null || userNameClaim is null)
            {
                Logger.LogDebug("Token validation passed but it does not contain user information. {Token}", request.AccessToken);
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Token does not contain user information."));
            }

            Logger.LogDebug("Token validation passed. {Token}", request.AccessToken);
            return new ValidateUserTokenResponse
            {
                IsValid = true,
                UserAccount = new UserAccount { UserId = userIdClaim.Value, UserName = userNameClaim.Value }
            };
        }

        Logger.LogDebug("Token validation failed. {Token}, Exception: {Exception}", request.AccessToken, validateResult.Exception.Message);
        return new ValidateUserTokenResponse { IsValid = false };
    }
}
