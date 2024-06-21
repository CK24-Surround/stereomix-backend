using Edgegap;
using StereoMix.Discord;
using StereoMix.Security;
using StereoMix.Storage;

namespace StereoMix.Grpc;

public partial class LobbyService : Lobby.LobbyService.LobbyServiceBase
{
    public LobbyService(
        ILogger<LobbyService> logger,
        IEdgegapClient edgegap,
        ILobbyStorage lobbyStorage,
        IJwtTokenProvider jwtTokenProvider,
        IRoomEncryptor roomEncryptor,
        DiscordMatchNotify discordMatchNotify)
    {
        Logger = logger;
        Edgegap = edgegap;
        LobbyStorage = lobbyStorage;
        JwtTokenProvider = jwtTokenProvider;
        RoomEncryptor = roomEncryptor;
        DiscordMatchNotifyService = discordMatchNotify;
    }

    protected ILogger<LobbyService> Logger { get; }
    protected IEdgegapClient Edgegap { get; }
    protected ILobbyStorage LobbyStorage { get; }
    protected IJwtTokenProvider JwtTokenProvider { get; }
    protected IRoomEncryptor RoomEncryptor { get; }
    protected DiscordMatchNotify DiscordMatchNotifyService { get; }
}
