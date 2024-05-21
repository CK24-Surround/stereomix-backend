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
    public override async Task CreateRoom(CreateRoomRequest request, IServerStreamWriter<CreateRoomResponse> responseStream, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        // var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(StereoMixClaimTypes.UserName) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        var requestIp = httpContext.Connection.RemoteIpAddress;
        if (requestIp is null)
        {
            throw new RpcException(new Status(StatusCode.Aborted, "Request IP not found."));
        }

        // Test
        // if (Equals(requestIp, IPAddress.Loopback))
        // {
        //     requestIp = IPAddress.Parse("121.157.127.18");
        // }
        // Temp
        requestIp = IPAddress.Parse("121.157.127.18");

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
        var serverAuthToken = JwtTokenProvider.AuthenticateGameServer(roomId);

        EdgegapDeploymentStatusType deploymentStatus;
        string deploymentId;
        RoomConnectionInfo? roomConnection;
        try
        {
            (deploymentStatus, deploymentId, roomConnection) = await CreateDeploymentAsync(serverAuthToken, requestIp.ToString()).ConfigureAwait(false);
            Logger.LogDebug("Deployment created. Status: {Status}, DeploymentId: {DeploymentId}", deploymentStatus, deploymentId);
        }
        catch (EdgegapException e)
        {
            Logger.LogError("Failed to create deployment. {Error}", e.ToString());
            if (e.Response.Errors != null)
            {
                foreach (var (name, message) in e.Response.Errors)
                {
                    Logger.LogTrace("{Name}: {Message}", name, message);
                }
            }

            throw new RpcException(new Status(StatusCode.Internal, "Failed to create deployment."));
        }

        if (roomConnection is null)
        {
            Logger.LogError("Deployment create failed. LastStatus: {Status}", deploymentStatus);
            throw new RpcException(new Status(StatusCode.Internal, "Deployment create failed."));
        }

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
            return;
        }

        Logger.LogError("Failed to save room data to db. ({Response})", storageResponse);
        throw new RpcException(new Status(StatusCode.Internal, "Failed to create room."));

        async ValueTask<(EdgegapDeploymentStatusType status, string deploymentId, RoomConnectionInfo? connection)> CreateDeploymentAsync(string authToken, string ip)
        {
            var createDeploymentRequest = new EdgegapCreateDeploymentRequest
            {
                AppName = Edgegap.Config.AppName,
                VersionName = Edgegap.Config.AppVersion,
                EnvVars =
                [
                    new EdgegapDeployEnvironment
                    {
                        Key = "STEREOMIX_AUTH_TOKEN",
                        Value = authToken,
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
                IpList = [ip],
                Tags = ["CustomRoom"]
            };

            var createDeploymentResponse = await Edgegap.CreateDeploymentAsync(createDeploymentRequest, context.CancellationToken).ConfigureAwait(false);
            var requestId = createDeploymentResponse.RequestId;
            Logger.LogDebug("New deployment created: RequestId={requestId},City={city}", requestId, createDeploymentResponse.City);

            var status = EdgegapDeploymentStatusType.Unspecified;
            RoomConnectionInfo? connection = null;

            var retryCount = 100;
            while (retryCount > 0)
            {
                var getStatusResponse = await Edgegap.GetDeploymentStatusAsync(requestId, context.CancellationToken).ConfigureAwait(false);
                status = getStatusResponse.CurrentStatus;

                Logger.LogDebug("Deployment status for {RequestId}: {Status}", requestId, status);

                var createRoomResponse = new CreateRoomResponse { DeployStatus = status.ToRoomDeploymentStatus() };
                if (status is EdgegapDeploymentStatusType.Ready or EdgegapDeploymentStatusType.Terminated or EdgegapDeploymentStatusType.Error)
                {
                    if (status is EdgegapDeploymentStatusType.Ready)
                    {
                        connection = new RoomConnectionInfo
                        {
                            Host = getStatusResponse.Fqdn,
                            Port = getStatusResponse.Ports?.GetValueOrDefault("Game")?.External ?? throw new InvalidOperationException("Game port not found.")
                        };
                        createRoomResponse.Connection = connection;
                    }

                    await responseStream.WriteAsync(createRoomResponse, context.CancellationToken).ConfigureAwait(false);
                    break;
                }

                await responseStream.WriteAsync(createRoomResponse, context.CancellationToken).ConfigureAwait(false);
                await Task.Delay(2000, context.CancellationToken).ConfigureAwait(false);

                retryCount++;
            }

            return (status, requestId, connection);
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
