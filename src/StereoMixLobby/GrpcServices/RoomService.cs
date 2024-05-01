using Grpc.Core;

namespace StereoMixLobby.GrpcServices;

using RoomCreateResult = CreateRoomResponse.Types.RoomCreateResult;
using RoomEnterResult = EnterRoomResponse.Types.RoomEnterResult;
using RoomLeaveResult = LeaveRoomResponse.Types.RoomLeaveResult;

public class RoomService(ILogger<RoomService> logger) : Room.RoomBase
{
    public override Task<CreateRoomResponse> CreateRoom(CreateRoomRequest request, ServerCallContext context)
    {
        return Task.FromResult(new CreateRoomResponse { Result = RoomCreateResult.InternalError, RoomId = null });
    }

    public override Task<EnterRoomResponse> EnterRoom(EnterRoomRequest request, ServerCallContext context)
    {
        return Task.FromResult(new EnterRoomResponse { Result = RoomEnterResult.InternalError });
    }

    public override Task<LeaveRoomResponse> LeaveRoom(LeaveRoomRequest request, ServerCallContext context)
    {
        return Task.FromResult(new LeaveRoomResponse { Result = RoomLeaveResult.InternalError });
    }

    public override Task<GetRoomListResponse> GetRoomList(GetRoomListRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetRoomListResponse
        {
            RoomList =
            {
                new RoomInfo { RoomId = "1", RoomName = "Test Room" },
                new RoomInfo { RoomId = "2", RoomName = "Test Room 2" }
            }
        });
    }
}
