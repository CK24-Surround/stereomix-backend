using Edgegap;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using StereoMix.Lobby;
using StereoMix.Storage;

namespace StereoMix.Grpc;

public partial class LobbyService
{
    [Authorize(Policy = StereoMixPolicy.AuthorizeGameServerOnlyPolicy)]
    public override async Task<UpdateRoomStateResponse> UpdateRoomState(UpdateRoomStateRequest request, ServerCallContext context)
    {
        var requestUser = context.GetHttpContext().User;
        var requestRoomId = requestUser.FindFirst(StereoMixClaimTypes.RoomId)?.Value ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "RoomId is required"));

        if (string.IsNullOrWhiteSpace(requestRoomId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "RoomId is required"));
        }

        if (request.State == RoomState.Unspecified)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "State is required"));
        }

        var updateResponse = await LobbyStorage.SetRoomAsync(requestRoomId, data => data.State = request.State, context.CancellationToken).ConfigureAwait(false);
        if (updateResponse == StorageResponse.NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Room not found"));
        }

        return new UpdateRoomStateResponse { UpdatedState = request.State };
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeGameServerOnlyPolicy)]
    public override async Task<ChangeRoomOwnerResponse> ChangeRoomOwner(ChangeRoomOwnerRequest request, ServerCallContext context)
    {
        var requestUser = context.GetHttpContext().User;
        var requestRoomId = requestUser.FindFirst(StereoMixClaimTypes.RoomId)?.Value ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "RoomId is required"));

        if (string.IsNullOrWhiteSpace(requestRoomId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "RoomId is required"));
        }

        if (string.IsNullOrWhiteSpace(request.NewOwnerId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "NewOwnerId is required"));
        }

        var updateResponse = await LobbyStorage.SetRoomAsync(requestRoomId, data => data.OwnerId = request.NewOwnerId, context.CancellationToken).ConfigureAwait(false);
        if (updateResponse == StorageResponse.NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Room not found"));
        }

        return new ChangeRoomOwnerResponse { OwnerId = request.NewOwnerId };
    }

    [Authorize(Policy = StereoMixPolicy.AuthorizeGameServerOnlyPolicy)]
    public override async Task<DeleteRoomResponse> DeleteRoom(DeleteRoomRequest request, ServerCallContext context)
    {
        var requestUser = context.GetHttpContext().User;
        var requestRoomId = requestUser.FindFirst(StereoMixClaimTypes.RoomId)?.Value ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "RoomId is required"));

        if (string.IsNullOrWhiteSpace(requestRoomId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "RoomId is required"));
        }

        string? deploymentId = null;
        var storageResponse = await LobbyStorage.SetRoomAsync(requestRoomId, data =>
        {
            deploymentId = data.DeploymentId;
            data.State = RoomState.Closed;
        }).ConfigureAwait(false);

        if (storageResponse == StorageResponse.NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Room not found"));
        }

        if (deploymentId is null)
        {
            Logger.LogError("DeploymentId not found for room {RoomId}", requestRoomId);
            throw new RpcException(new Status(StatusCode.Internal, "DeploymentId not found"));
        }

        try
        {
            var deleteDeploymentRespons = await Edgegap.DeleteDeploymentAsync(deploymentId, context.CancellationToken).ConfigureAwait(false);
            Logger.LogInformation("Deployment {DeploymentId} delete response: {Message}", deploymentId, deleteDeploymentRespons.Message);

            return new DeleteRoomResponse();
        }
        catch (EdgegapException e)
        {
            Logger.LogError("Failed to delete deployment. {Error}", e.ToString());
            if (e.Response.Errors != null)
            {
                foreach (var (name, message) in e.Response.Errors)
                {
                    Logger.LogTrace("{Name}: {Message}", name, message);
                }
            }

            throw new RpcException(new Status(StatusCode.Internal, "Failed to delete deployment"));
        }
    }
}
