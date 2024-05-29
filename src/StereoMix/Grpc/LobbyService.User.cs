using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Edgegap;
using Edgegap.Model;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using StereoMix.Lobby;
using StereoMix.Security;
using StereoMix.Storage;

namespace StereoMix.Grpc;

public partial class LobbyService
{
    [Authorize(Policy = StereoMixPolicy.AuthorizeUserOnlyPolicy)]
    public override async Task<CreateRoomResponse> CreateRoom(CreateRoomRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        var _ = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(StereoMixClaimTypes.UserName) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        var requestIp = httpContext.Connection.RemoteIpAddress;

        if (requestIp is null)
        {
            Logger.LogWarning("Request IP not found. Set request ip to default.");
            requestIp = IPAddress.Loopback;
        }

        // For Test
        if (Equals(requestIp, IPAddress.Loopback))
        {
            requestIp = IPAddress.Parse("121.157.127.18"); // 청강대 아이피
        }

        Logger.LogDebug("User {User} from {ip} is request creating a room.", userName, requestIp);

        if (string.IsNullOrWhiteSpace(request.RoomName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room name cannot be empty."));
        }

        if (request.RoomName.Length > 32)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room name is too long."));
        }

        var roomId = IdGenerator.GenerateRoomId();
        var shortRoomId = IdGenerator.GenerateShortRoomId();
        var gameServerAuthToken = JwtTokenProvider.AuthenticateGameServer(roomId);

        string deploymentId;

        try
        {
            var response = await CreateDeploymentAsync().ConfigureAwait(false);
            Logger.LogDebug(
                "Deployment creating: RequestId={RequestId}, AppVersion={AppVersion}, Location={City}, {Country}, {Continent}",
                response.RequestId, response.RequestVersion, response.City, response.Country, response.Continent);

            deploymentId = response.RequestId;
        }
        catch (TaskCanceledException)
        {
            Logger.LogWarning("Deployment creation cancelled.");
            throw new RpcException(new Status(StatusCode.Cancelled, "Deployment creation cancelled."));
        }
        catch (EdgegapException e)
        {
            Logger.LogError("Failed to create deployment. {Error}", e.ToString());
            if (e.Response.Errors == null)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Failed to create deployment."));
            }

            foreach (var (name, message) in e.Response.Errors)
            {
                Logger.LogTrace("{Name}: {Message}", name, message);
            }

            throw new RpcException(new Status(StatusCode.Internal, "Failed to create deployment."));
        }

        EdgegapDeploymentStatus? deploymentStatus = null;
        try
        {
            // repeat until deployment is ready
            var retryCount = 50;

            while (retryCount > 0)
            {
                var response = await Edgegap.GetDeploymentStatusAsync(deploymentId, context.CancellationToken).ConfigureAwait(false);
                Logger.LogDebug("Deployment status for {RequestId}: {Status}", response.RequestId, response.CurrentStatus.ToString());

                if (response.CurrentStatus is EdgegapDeploymentStatusType.Ready)
                {
                    deploymentStatus = response;
                    break;
                }

                await Task.Delay(2000, context.CancellationToken).ConfigureAwait(false);

                retryCount--;
            }

            if (retryCount == 0)
            {
                Logger.LogWarning("Deployment status is not ready.");
                throw new RpcException(new Status(StatusCode.DeadlineExceeded, "Deployment status is not ready."));
            }
        }
        catch (TaskCanceledException)
        {
            Logger.LogWarning("Deployment creation cancelled.");
            throw new RpcException(new Status(StatusCode.Cancelled, "Deployment creation cancelled."));
        }
        catch (EdgegapException e)
        {
            Logger.LogError(e, "Failed to create deployment when get deployment status");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to create deployment."));
        }
        finally
        {
            if (deploymentStatus is null)
            {
                Logger.LogWarning("Deployment status is null. Deleting deployment.");
                await Edgegap.DeleteDeploymentAsync(deploymentId, context.CancellationToken).ConfigureAwait(false);
            }
        }

        Debug.Assert(deploymentStatus != null, nameof(deploymentStatus) + "is null.");

        var roomConnection = new RoomConnectionInfo
        {
            Host = deploymentStatus.Fqdn,
            Port = deploymentStatus.Ports?.GetValueOrDefault("Game")?.External ?? throw new InvalidOperationException("Game port not found.")
        };

        var roomData = new LobbyStorageData
        {
            RoomId = roomId,
            ShortId = shortRoomId,
            RoomName = request.RoomName,
            PasswordEncrypted = request.Config.Visibility == RoomVisibility.Private ? RoomEncryptor.HashPassword(roomId, request.Password) : string.Empty,
            Visibility = request.Config.Visibility != RoomVisibility.Unspecified ? request.Config.Visibility : RoomVisibility.Public,
            Mode = request.Config.Mode != GameMode.Unspecified ? request.Config.Mode : GameMode.Default,
            Map = request.Config.Map != GameMap.Unspecified ? request.Config.Map : GameMap.Default,
            MaxPlayers = 6,
            CurrentPlayers = 0,
            OwnerId = null,
            DeploymentId = deploymentId,
            Connection = new LobbyStorageConnectionData
            {
                Ip = roomConnection.Host,
                Port = roomConnection.Port
            },
            State = RoomState.Open
        };

        var storageResponse = await LobbyStorage.CreateRoomAsync(roomData, context.CancellationToken).ConfigureAwait(false);
        if (storageResponse is StorageResponse.Success)
        {
            return new CreateRoomResponse { Connection = roomConnection };
        }

        Logger.LogError("Failed to save room data to db. ({Response})", storageResponse);

        await Edgegap.DeleteDeploymentAsync(deploymentId, context.CancellationToken).ConfigureAwait(false);
        throw new RpcException(new Status(StatusCode.Internal, "Failed to create room."));

        Task<EdgegapCreateDeploymentResponse> CreateDeploymentAsync()
        {
            var createDeploymentRequest = new EdgegapCreateDeploymentRequest
            {
                AppName = Edgegap.Config.AppName,
                // VersionName = Edgegap.Config.AppVersion,
                EnvVars =
                [
                    new EdgegapDeployEnvironment
                    {
                        Key = "STEREOMIX_AUTH_TOKEN",
                        Value = gameServerAuthToken,
                        IsHidden = true
                    },
                    new EdgegapDeployEnvironment
                    {
                        Key = "STEREOMIX_ROOM_ID",
                        Value = roomId,
                        IsHidden = false
                    },
                    new EdgegapDeployEnvironment
                    {
                        Key = "STEREOMIX_SHORT_ROOM_ID",
                        Value = shortRoomId,
                        IsHidden = false
                    }
                ],
                Filters =
                [
                    new EdgegapDeploymentFilter
                    {
                        Field = EdgegapDeploymentFilterFieldType.Country,
                        FilterType = EdgegapDeploymentFilterType.Any,
                        Values = ["South Korea", "Japan"]
                    }
                ],
                IpList = [requestIp.ToString()],
                Tags = ["CustomRoom"]
            };

            return Edgegap.CreateDeploymentAsync(createDeploymentRequest, context.CancellationToken);
        }
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeUserOnlyPolicy)]
    public override async Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        // var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(StereoMixClaimTypes.UserName) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        var requestIp = httpContext.Request.Host.Host;
        Logger.LogDebug("User {User} from {ip} is request joining a room.", userName, requestIp);

        LobbyStorageData? roomData;
        if (request.IdCase == JoinRoomRequest.IdOneofCase.RoomId)
        {
            if (!request.HasRoomId || string.IsNullOrWhiteSpace(request.RoomId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Room Id is invalid."));
            }

            roomData = await LobbyStorage.GetRoomAsync(request.RoomId, context.CancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (!request.HasShortRoomId || string.IsNullOrWhiteSpace(request.ShortRoomId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Short Room Id is invalid."));
            }

            roomData = await LobbyStorage.FindRoomByShortIdAsync(request.ShortRoomId, context.CancellationToken).ConfigureAwait(false);
        }

        if (roomData is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Room not found."));
        }

        if (roomData.Visibility == RoomVisibility.Private)
        {
            var verifyResult = RoomEncryptor.VerifyHashedPassword(roomData.RoomId, request.Password, roomData.PasswordEncrypted!);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid password."));
            }
        }

        if (roomData.CurrentPlayers >= roomData.MaxPlayers)
        {
            throw new RpcException(new Status(StatusCode.Aborted, "Room is full."));
        }

        var connection = roomData.Connection;
        if (connection is null)
        {
            throw new RpcException(new Status(StatusCode.Internal, "Room connection data not found."));
        }

        return new JoinRoomResponse
        {
            Connection = new RoomConnectionInfo
            {
                Host = connection.Ip,
                Port = connection.Port
            }
        };
    }
}
