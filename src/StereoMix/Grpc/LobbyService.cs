using System.Security.Claims;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using StereoMix.Edgegap;
using StereoMix.Firestore;
using StereoMix.Hathora;
using StereoMix.Hathora.Model;
using StereoMix.Lobby;
using StereoMix.Room;
using StereoMix.Security;
using CreateRoomRequest = StereoMix.Lobby.CreateRoomRequest;
using CreateRoomResponse = StereoMix.Lobby.CreateRoomResponse;

namespace StereoMix.Grpc;

public class LobbyService(
    ILogger<LobbyService> logger,
    IFirestoreService firestore,
    IEdgegapService edgegap,
    IHathoraCloudService hathora,
    IRoomEncryptService roomEncryptor) : Lobby.LobbyService.LobbyServiceBase
{
    [Authorize(Policy = "UserPolicy")]
    public override async Task<CreateRoomResponse> CreateRoom(CreateRoomRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(ClaimTypes.Name) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        var requestIp = httpContext.Request.Host.Host;
        logger.LogInformation("User {User} from {ip} is request creating a room.", userName, requestIp);

        if (string.IsNullOrWhiteSpace(request.Config.RoomName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room name cannot be empty."));
        }

        if (request.Config.RoomName.Length > 32)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room name is too long."));
        }


        var roomName = request.Config.RoomName;
        var roomId = IdGenerator.GenerateRoomId();
        var roomPassword = request.Config.Visibility == RoomVisibility.Private ? roomEncryptor.HashPassword(roomId, request.Password) : string.Empty;
        var visibility = request.Config.Visibility == RoomVisibility.Unspecified ? RoomVisibility.Public : request.Config.Visibility;
        var mode = request.Config.Mode == GameMode.Unspecified ? GameMode.Default : request.Config.Mode;
        var map = request.Config.Map == GameMap.Unspecified ? GameMap.Default : request.Config.Map;

        // Firestore
        var db = await firestore.GetDatabaseAsync().ConfigureAwait(false);
        var rooms = db.Collection("rooms");
        DocumentReference documentRef;

        bool idExists;
        do
        {
            roomId = IdGenerator.GenerateRoomId();
            documentRef = rooms.Document(roomId);
            var snapshot = await documentRef.GetSnapshotAsync(context.CancellationToken).ConfigureAwait(false);
            idExists = snapshot.Exists;
        } while (idExists);

        logger.LogDebug("New room id generated: {RoomId}", roomId);

        Connection connection;
        try
        {
            var createRoomRequest = new Hathora.Model.CreateRoomRequest { RoomId = roomId, RoomConfig = roomId, Region = HathoraRegion.Tokyo };
            var createRoomResponse = await hathora.CreateRoomAsync(createRoomRequest, context.CancellationToken).ConfigureAwait(false);

            var host = createRoomResponse.ExposedPort?.Host ?? throw new InvalidOperationException("Host not found.");
            var port = createRoomResponse.ExposedPort.Port;
            connection = new Connection { Host = host, Port = port };
            logger.LogInformation("New game server process created: {Host}:{Port}", host, port);
        }
        catch (HttpRequestException e)
        {
            throw new RpcException(new Status(StatusCode.Aborted, $"CreateRoom failed ({e.StatusCode}): {e.Message}"));
        }

        var roomData = new Dictionary<string, object>
        {
            ["room_id"] = roomId,
            ["password_encrypted"] = roomPassword,
            ["state"] = RoomState.Waiting.ToString(),
            ["owner_id"] = string.Empty,
            ["config"] = new Dictionary<string, object>
            {
                ["room_name"] = roomName,
                ["visibility"] = visibility.ToString(),
                ["mode"] = mode,
                ["map"] = map,
                ["max_players"] = 6
            },
            ["players"] = new List<Dictionary<string, object>>(),
            ["player_count"] = 0,
            ["connection"] = new Dictionary<string, object> { ["host"] = connection.Host, ["port"] = connection.Port }
        };
        await documentRef.SetAsync(roomData, SetOptions.Overwrite, context.CancellationToken).ConfigureAwait(false);

        logger.LogInformation("Room {RoomId} is created by {UserId}.", roomId, userName);
        return new CreateRoomResponse { Connection = connection };
    }

    [Authorize(Policy = "UserPolicy")]
    public override async Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(ClaimTypes.Name) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        var requestIp = httpContext.Request.Host.Host;
        logger.LogInformation("User {User} from {ip} is request joining a room.", userName, requestIp);

        var requestRoomId = request.RoomId;
        if (string.IsNullOrWhiteSpace(requestRoomId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room Id cannot be empty."));
        }

        var db = await firestore.GetDatabaseAsync().ConfigureAwait(false);

        var connection = await db.RunTransactionAsync<Connection>(async transaction =>
        {
            var rooms = db.Collection("rooms");

            var documentRef = rooms.Document(requestRoomId);
            var snapshot = await transaction.GetSnapshotAsync(documentRef, context.CancellationToken).ConfigureAwait(true);
            if (!snapshot.Exists)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Room not found."));
            }

            var roomData = snapshot.ConvertTo<Dictionary<string, object>>();
            var config = roomData["config"] as Dictionary<string, object> ?? throw new InvalidOperationException("Room config not found.");

            var visibility = Enum.Parse<RoomVisibility>(config["visibility"].ToString() ?? string.Empty);
            if (visibility == RoomVisibility.Private)
            {
                var roomPassword = roomData["password_encrypted"] as string ?? throw new InvalidOperationException("Room password not found.");
                var verified = roomEncryptor.VerifyHashedPassword(requestRoomId, request.Password, roomPassword);
                if (verified == PasswordVerificationResult.Failed)
                {
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid password."));
                }
            }

            var players = roomData["players"] as List<Dictionary<string, object>> ?? [];
            if (players.Count >= (int)(long)config["max_players"])
            {
                throw new RpcException(new Status(StatusCode.Aborted, "Room is full."));
            }

            var isOwner = players.Count == 0;
            players.Add(new Dictionary<string, object>
            {
                ["user_id"] = userId,
                ["user_name"] = userName,
                ["state"] = PlayerState.Joining,
                ["is_owner"] = isOwner,
                ["joined_at"] = DateTime.UtcNow
            });

            if (isOwner)
            {
                transaction.Update(documentRef, "owner_id", userId);
            }

            transaction.Update(documentRef, "players", players);

            var connectionRaw = roomData["connection"] as Dictionary<string, object> ?? throw new InvalidOperationException("Room connection not found.");
            var connection = new Connection { Host = connectionRaw["host"] as string ?? throw new InvalidOperationException("Host not found."), Port = (int)(connectionRaw["port"] as long? ?? throw new InvalidOperationException("Port not found.")) };
            return connection;
        }, cancellationToken: context.CancellationToken).ConfigureAwait(false);

        return new JoinRoomResponse { Connection = connection };
    }

    /*
    [Authorize(Policy = "UserPolicy")]
    public override async Task<GameServerInfo> CreateRoom(CreateRoomRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        _ = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(ClaimTypes.Name) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        var requestIp = httpContext.Request.Host.Host;
        logger.LogInformation("User {User} from {ip} is request creating a room.", userName, requestIp);

        if (string.IsNullOrWhiteSpace(request.RoomName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room name cannot be empty."));
        }

        if (request.RoomName.Length > 32)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Room name is too long."));
        }

        var roomId = Guid.NewGuid().ToString("N");
        var roomPassword = request.Visibility == RoomVisibility.Private ? roomEncryptor.HashPassword(roomId, request.Password) : string.Empty;

        try
        {
            // Edgegap deployment
            var deploymentRequest = new CreateDeploymentRequest
            {
                AppName = "stereomix",
                VersionName = "demo-v1.0",
                //EnvVars =
                //[
                //    new DeployEnvironment { Key = "ROOM_ID", Value = roomId },
                //    new DeployEnvironment { Key = "ROOM_PASSWORD", Value = roomPassword, IsHidden = true },
                //    new DeployEnvironment { Key = "MATCH_TYPE", Value = "Room" }
                //],
                IpList = ["180.224.212.10"]
            };
            var deploymentStatus = await edgegap.CreateDeploymentAsync(deploymentRequest, context.CancellationToken).ConfigureAwait(false);

            var serverInfo = deploymentStatus.Ports?["Game"] ?? throw new InvalidOperationException("Game port is not found.");
            var address = deploymentStatus.Fqdn;
            var port = serverInfo.External;

            // Firestore
            var db = await firestore.GetDatabaseAsync().ConfigureAwait(false);
            var rooms = db.Collection("rooms");

            var roomInfo = new RoomInfo
            {
                RoomId = roomId,
                RoomName = request.RoomName,
                PasswordEncrypted = roomPassword,
                Visibility = request.Visibility,
                Mode = request.Mode,
                Map = request.Map,
                MaxPlayers = request.MaxPlayers,
                Status = RoomStatus.Waiting,
                GameServerAddress = address,
                GameServerPort = port
            };

            var documentRef = rooms.Document(roomId);
            await documentRef.SetAsync(roomInfo, SetOptions.Overwrite, context.CancellationToken).ConfigureAwait(false);

            logger.LogInformation("Room {RoomId} is created by {User}.", roomId, userName);
            return new CreateRoomResponse { CreatedRoomId = roomId };
        }
        catch (Exception e)
        {
            throw new RpcException(new Status(StatusCode.Internal, e.Message, e));
        }
    }

    [Authorize(Policy = "UserPolicy")]
    public override Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request, ServerCallContext context)
    {
        return base.JoinRoom(request, context);
    }

    [Authorize(Policy = "UserPolicy")]
    public override async Task<GetRoomListResponse> GetRoomList(GetRoomListRequest request, ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User Id not found."));
        var userName = user.FindFirstValue(ClaimTypes.Name) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User name not found."));
        logger.LogInformation("User {User} is request getting room list.", userName);

        var db = await firestore.GetDatabaseAsync().ConfigureAwait(false);
        var rooms = db.Collection("rooms");
        var query = rooms.WhereEqualTo("Status", RoomStatus.Waiting);
        if (request.Visibility != RoomVisibility.Unspecified)
        {
            query = query.WhereEqualTo("Visibility", request.Visibility);
        }

        if (request.Mode != GameMode.Unspecified)
        {
            query = query.WhereEqualTo("Mode", request.Mode);
        }

        if (request.Map != GameMap.Unspecified)
        {
            query = query.WhereEqualTo("Map", request.Map);
        }

        var snapshot = await query.GetSnapshotAsync(context.CancellationToken).ConfigureAwait(false);
        var roomList = snapshot.Documents.Select(doc => doc.ConvertTo<RoomInfo>()).ToList<>();
        logger.LogInformation("User {User} got {RoomCount} rooms.", userName, roomList.Count);

        var response = new GetRoomListResponse();
        foreach (var roomInfo in roomList)
        {
            response.RoomList.Add(new LobbyRoomInfo
            {
                RoomId = roomInfo.RoomId,
                RoomName = roomInfo.RoomName,
                Visibility = roomInfo.Visibility,
                Mode = roomInfo.Mode,
                Map = roomInfo.Map,
                MaxPlayers = roomInfo.MaxPlayers,
                CurrentPlayers = roomInfo.Players.Count
            });
        }

        return response;
    }
    */
}
