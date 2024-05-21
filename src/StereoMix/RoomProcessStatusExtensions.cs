using Edgegap.Model;
using StereoMix.Lobby;

namespace StereoMix;

public static class RoomProcessStatusExtensions
{
    public static RoomDeploymentStatus ToRoomDeploymentStatus(this EdgegapDeploymentStatusType status)
    {
        return status switch
        {
            EdgegapDeploymentStatusType.Unspecified => RoomDeploymentStatus.Unspecified,
            EdgegapDeploymentStatusType.Initializing => RoomDeploymentStatus.Initializing,
            EdgegapDeploymentStatusType.Seeking => RoomDeploymentStatus.Seeking,
            EdgegapDeploymentStatusType.Seeked => RoomDeploymentStatus.Seeked,
            EdgegapDeploymentStatusType.Scanning => RoomDeploymentStatus.Scanning,
            EdgegapDeploymentStatusType.Deploying => RoomDeploymentStatus.Deploying,
            EdgegapDeploymentStatusType.Ready => RoomDeploymentStatus.Ready,
            EdgegapDeploymentStatusType.Terminated => RoomDeploymentStatus.Terminated,
            EdgegapDeploymentStatusType.Error => RoomDeploymentStatus.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public static EdgegapDeploymentStatusType ToEdgegapDeploymentStatusType(this RoomDeploymentStatus status)
    {
        return status switch
        {
            RoomDeploymentStatus.Unspecified => EdgegapDeploymentStatusType.Unspecified,
            RoomDeploymentStatus.Initializing => EdgegapDeploymentStatusType.Initializing,
            RoomDeploymentStatus.Seeking => EdgegapDeploymentStatusType.Seeking,
            RoomDeploymentStatus.Seeked => EdgegapDeploymentStatusType.Seeked,
            RoomDeploymentStatus.Scanning => EdgegapDeploymentStatusType.Scanning,
            RoomDeploymentStatus.Deploying => EdgegapDeploymentStatusType.Deploying,
            RoomDeploymentStatus.Ready => EdgegapDeploymentStatusType.Ready,
            RoomDeploymentStatus.Terminated => EdgegapDeploymentStatusType.Terminated,
            RoomDeploymentStatus.Error => EdgegapDeploymentStatusType.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}
