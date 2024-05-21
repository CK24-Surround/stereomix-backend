using Edgegap;
using StereoMix.Security;
using StereoMix.Storage;

namespace StereoMix.Grpc;

public partial class LobbyService(ILogger<LobbyService> logger, IEdgegapClient edgegap, ILobbyStorage lobbyStorage, IJwtTokenProvider jwtTokenProvider, IRoomEncryptor roomEncryptor)
    : Lobby.LobbyService.LobbyServiceBase
{
    protected ILogger<LobbyService> Logger { get; } = logger;
    protected IEdgegapClient Edgegap { get; } = edgegap;
    protected ILobbyStorage LobbyStorage { get; } = lobbyStorage;
    protected IJwtTokenProvider JwtTokenProvider { get; } = jwtTokenProvider;
    protected IRoomEncryptor RoomEncryptor { get; } = roomEncryptor;
}
