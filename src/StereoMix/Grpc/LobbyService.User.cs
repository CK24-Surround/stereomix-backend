using System.Diagnostics;
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
        var userId = user.FindFirstValue(StereoMixClaimTypes.UserId) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(StereoMixClaimTypes.UserName) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        Logger.LogDebug("User {User}({UserId}) is request creating a room.", userName, userId);

        if (string.IsNullOrWhiteSpace(request.RoomName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room name is empty."));
        }

        if (request.RoomName.Length > 32)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room name is too long."));
        }

        if (string.IsNullOrWhiteSpace(request.GameVersion))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Game version is empty."));
        }

        var roomId = IdGenerator.GenerateRoomId();
        var shortRoomId = IdGenerator.GenerateShortRoomId();
        var gameServerAuthToken = JwtTokenProvider.AuthenticateGameServer(roomId);

        EdgegapCreateDeploymentResponse? createDeploymentResponse;
        try
        {
            createDeploymentResponse = await CreateDeploymentAsync(context, request.GameVersion, gameServerAuthToken, roomId, shortRoomId).ConfigureAwait(false);
            Logger.LogInformation(
                "Deployment creating: RequestId={RequestId}, AppVersion={AppVersion}, Location={City}, {Country}, {Continent}",
                createDeploymentResponse.RequestId, createDeploymentResponse.RequestVersion, createDeploymentResponse.City, createDeploymentResponse.Country, createDeploymentResponse.Continent);
        }
        catch (TaskCanceledException)
        {
            Logger.LogWarning("Deployment creation cancelled.");
            throw new RpcException(new Status(StatusCode.Cancelled, "Deployment creation cancelled."));
        }
        catch (EdgegapException e)
        {
            Logger.LogError("Failed to create deployment. {Error}", e.ToString());
            if (e.Response.Errors is null)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Failed to create deployment."));
            }

            foreach (var (name, message) in e.Response.Errors)
            {
                Logger.LogTrace("{Name}: {Message}", name, message);
            }

            throw new RpcException(new Status(StatusCode.Internal, "Failed to create deployment."));
        }

        var deploymentId = createDeploymentResponse.RequestId;

        EdgegapDeploymentStatus? deploymentStatus = null;
        try
        {
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

                if (response.CurrentStatus is EdgegapDeploymentStatusType.Error)
                {
                    Logger.LogError("Deployment status is error.");
                    throw new RpcException(new Status(StatusCode.Internal, "Deployment status is error."));
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
                await Edgegap.DeleteDeploymentAsync(deploymentId).ConfigureAwait(false);
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
            GameVersion = deploymentStatus.AppVersion,
            ShortId = shortRoomId,
            RoomName = request.RoomName,
            DeploymentId = deploymentId,
            Connection = new LobbyStorageConnectionData
            {
                Ip = roomConnection.Host,
                Port = roomConnection.Port
            },
            State = RoomState.Open,
            MaxPlayers = 6
        };

        var storageResponse = await LobbyStorage.CreateRoomAsync(roomData, context.CancellationToken).ConfigureAwait(false);
        if (storageResponse is not StorageResponse.Success)
        {
            Logger.LogError("Failed to save room data to db. ({Response})", storageResponse);

            await Edgegap.DeleteDeploymentAsync(deploymentId, context.CancellationToken).ConfigureAwait(false);
            throw new RpcException(new Status(StatusCode.Internal, "Failed to create room."));
        }

        Logger.LogInformation("Room created - {RoomId} / {RoomName} / {ShortRoomId}", roomId, request.RoomName, shortRoomId);

        _ = DiscordMatchNotifyService.NotifyRoomCreated(userName, deploymentStatus.AppVersion, shortRoomId);

        return new CreateRoomResponse { Connection = roomConnection };
    }

    private async Task<EdgegapCreateDeploymentResponse> CreateDeploymentAsync(ServerCallContext context, string gameVersion, string gameServerAuthToken, string roomId, string shortRoomId)
    {
        var createDeploymentRequest = new EdgegapCreateDeploymentRequest
        {
            AppName = Edgegap.Config.AppName,
            IpList = ["121.157.127.18"], // 임시로 청강대 아이피 사용
            VersionName = gameVersion,
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
                new EdgegapDeployEnvironment // Obsolete
                {
                    Key = "STEREOMIX_SHORT_ROOM_ID",
                    Value = shortRoomId,
                    IsHidden = false
                },
                new EdgegapDeployEnvironment
                {
                    Key = "STEREOMIX_ROOM_CODE",
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
                    Values = ["South Korea" /*, "Japan"*/]
                }
            ],
            Tags = ["CustomRoom", shortRoomId]
        };

        var response = await Edgegap.CreateDeploymentAsync(createDeploymentRequest, context.CancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Deployment created - {AppName} / {VersionName} / {RoomId} / {ShortRoomId}", createDeploymentRequest.AppName, createDeploymentRequest.VersionName, roomId, shortRoomId);

        return response;
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeUserOnlyPolicy)]
    public override async Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        var userId = user.FindFirstValue(StereoMixClaimTypes.UserId) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(StereoMixClaimTypes.UserName) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        Logger.LogDebug("User {UserName}({UserId}) is request {FunctionName}.", userName, userId, nameof(JoinRoom));

        if (string.IsNullOrWhiteSpace(request.RoomId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room Id is invalid."));
        }

        if (string.IsNullOrWhiteSpace(request.GameVersion))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Game version is invalid."));
        }

        var roomData = await LobbyStorage.GetRoomAsync(request.RoomId, context.CancellationToken).ConfigureAwait(false);
        if (roomData is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Room not found."));
        }

        if (roomData.GameVersion != request.GameVersion)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game version mismatched."));
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

        Logger.LogInformation("User {UserName}({UserId}) joined room {RoomId}.", userName, userId, request.RoomId);

        _ = DiscordMatchNotifyService.NotifyRoomEntered(userName, roomData.GameVersion, roomData.RoomName, roomData.ShortId);

        return new JoinRoomResponse
        {
            Connection = new RoomConnectionInfo
            {
                Host = connection.Ip,
                Port = connection.Port
            }
        };
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeUserOnlyPolicy)]
    public override async Task<JoinRoomWithCodeResponse> JoinRoomWithCode(JoinRoomWithCodeRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        var userId = user.FindFirstValue(StereoMixClaimTypes.UserId) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(StereoMixClaimTypes.UserName) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        Logger.LogDebug("User {UserName}({UserId}) is request {FunctionName}.", userName, userId, nameof(JoinRoomWithCode));

        if (string.IsNullOrWhiteSpace(request.RoomCode))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Short Room Id is invalid."));
        }

        if (string.IsNullOrWhiteSpace(request.GameVersion))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Game version is invalid."));
        }

        var roomData = await LobbyStorage.FindRoomByShortIdAsync(request.GameVersion, request.RoomCode, context.CancellationToken).ConfigureAwait(false);
        if (roomData is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Room not found."));
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

        Logger.LogInformation("User {UserName}({UserId}) joined room {RoomId}.", userName, userId, roomData.RoomId);

        _ = DiscordMatchNotifyService.NotifyRoomEntered(userName, roomData.GameVersion, roomData.RoomName, roomData.ShortId);

        return new JoinRoomWithCodeResponse { Connection = new RoomConnectionInfo { Host = connection.Ip, Port = connection.Port } };
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeUserOnlyPolicy)]
    public override async Task<QuickMatchResponse> QuickMatch(QuickMatchRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        var userId = user.FindFirstValue(StereoMixClaimTypes.UserId) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(StereoMixClaimTypes.UserName) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        Logger.LogDebug("User {UserName}({UserId}) is request {FunctionName}.", userName, userId, nameof(QuickMatch));

        if (string.IsNullOrWhiteSpace(request.GameVersion))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Game version is invalid."));
        }

        var activeRooms = await LobbyStorage.GetRoomsAsync(request.GameVersion, GameMode.Unspecified, GameMap.Unspecified, context.CancellationToken).ConfigureAwait(false);
        var room = activeRooms.FirstOrDefault(r => r is { Visibility: RoomVisibility.Public, State: RoomState.Open } && r.CurrentPlayers < r.MaxPlayers);
        if (room?.Connection is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Room not found."));
        }

        var response = new QuickMatchResponse
        {
            Connection = new RoomConnectionInfo
            {
                Host = room.Connection.Ip,
                Port = room.Connection.Port
            }
        };

        _ = DiscordMatchNotifyService.NotifyRoomEntered(userName, room.GameVersion, room.RoomName, room.ShortId);

        return response;
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeUserOnlyPolicy)]
    public override async Task<GetRoomListResponse> GetRoomList(GetRoomListRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        var userId = user.FindFirstValue(StereoMixClaimTypes.UserId) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(StereoMixClaimTypes.UserName) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        Logger.LogDebug("User {UserName}({UserId}) is request {FunctionName}.", userName, userId, nameof(GetRoomList));

        if (string.IsNullOrWhiteSpace(request.GameVersion))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Game version is invalid."));
        }

        var activeRooms = await LobbyStorage.GetRoomsAsync(request.GameVersion, request.Mode, request.Map, context.CancellationToken).ConfigureAwait(false);
        var response = new GetRoomListResponse();
        foreach (var room in activeRooms)
        {
            var roomInfo = new Room
            {
                RoomId = room.RoomId,
                RoomCode = room.ShortId,
                OwnerId = room.OwnerId,
                Config = new RoomConfig
                {
                    RoomName = room.RoomName,
                    Visibility = room.Visibility,
                    Mode = room.Mode,
                    Map = room.Map
                },
                State = room.State,
                MaxPlayers = room.MaxPlayers,
                CurrentPlayers = room.CurrentPlayers,
                GameVersion = room.GameVersion
            };
            if (room.Connection is not null)
            {
                roomInfo.Connection = new RoomConnectionInfo { Host = room.Connection.Ip, Port = room.Connection.Port };
            }

            response.Rooms.Add(roomInfo);
        }

        return response;
    }
}
