using Google.Cloud.Firestore;
using StereoMix.Lobby;

namespace StereoMix.Storage;

[FirestoreData]
public class LobbyStorageData
{
    [FirestoreProperty("id")] public required string RoomId { get; init; }
    [FirestoreProperty("game_version")] public required string GameVersion { get; set; }
    [FirestoreProperty("short_id")] public required string ShortId { get; init; }
    [FirestoreProperty("name")] public required string RoomName { get; init; }
    [FirestoreProperty("password")] public string? PasswordEncrypted { get; set; }
    [FirestoreProperty("owner_id")] public string? OwnerId { get; set; }

    [FirestoreProperty("state", ConverterType = typeof(RoomStateEnumConverter))]
    public RoomState State { get; set; }

    [FirestoreProperty("visibility", ConverterType = typeof(RoomVisibilityEnumConverter))]
    public RoomVisibility Visibility { get; set; }

    [FirestoreProperty("mode", ConverterType = typeof(GameModeEnumConverter))]
    public GameMode Mode { get; set; }

    [FirestoreProperty("map", ConverterType = typeof(GameMapEnumConverter))]
    public GameMap Map { get; set; }

    [FirestoreProperty("max_players")] public int MaxPlayers { get; set; }
    [FirestoreProperty("current_players")] public int CurrentPlayers { get; set; }
    [FirestoreProperty("arbitrium_id")] public string? DeploymentId { get; set; }
    [FirestoreProperty("connection")] public LobbyStorageConnectionData? Connection { get; set; }


    public override string ToString()
    {
        return $"RoomId: {RoomId}, GameVersion: {GameVersion}, ShortId: {ShortId}, RoomName: {RoomName}, PasswordEncrypted: {PasswordEncrypted}, OwnerId: {OwnerId}, State: {State}, Visibility: {Visibility}, Mode: {Mode}, Map: {Map}, MaxPlayers: {MaxPlayers}, CurrentPlayers: {CurrentPlayers}, DeploymentId: {DeploymentId}, Connection: {Connection}";
    }
}

[FirestoreData]
public class LobbyStorageConnectionData
{
    [FirestoreProperty("ip")] public required string Ip { get; init; }
    [FirestoreProperty("port")] public required int Port { get; init; }
}

public class RoomStateEnumConverter : IFirestoreConverter<RoomState>
{
    public object ToFirestore(RoomState value)
    {
        return value switch
        {
            RoomState.Unspecified => "unspecified",
            RoomState.Open => "open",
            RoomState.Playing => "playing",
            RoomState.Closed => "closed",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public RoomState FromFirestore(object value)
    {
        return value switch
        {
            "unspecified" => RoomState.Unspecified,
            "open" => RoomState.Open,
            "playing" => RoomState.Playing,
            "closed" => RoomState.Closed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}

public class RoomVisibilityEnumConverter : IFirestoreConverter<RoomVisibility>
{
    public object ToFirestore(RoomVisibility value)
    {
        return value switch
        {
            RoomVisibility.Unspecified => "unspecified",
            RoomVisibility.Public => "public",
            RoomVisibility.Private => "private",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public RoomVisibility FromFirestore(object value)
    {
        return value switch
        {
            "unspecified" => RoomVisibility.Unspecified,
            "public" => RoomVisibility.Public,
            "private" => RoomVisibility.Private,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}

public class GameModeEnumConverter : IFirestoreConverter<GameMode>
{
    public object ToFirestore(GameMode value)
    {
        return value switch
        {
            GameMode.Unspecified => "unspecified",
            GameMode.Default => "default",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public GameMode FromFirestore(object value)
    {
        return value switch
        {
            "unspecified" => GameMode.Unspecified,
            "default" => GameMode.Default,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}

public class GameMapEnumConverter : IFirestoreConverter<GameMap>
{
    public object ToFirestore(GameMap value)
    {
        return value switch
        {
            GameMap.Unspecified => "unspecified",
            GameMap.Default => "default",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public GameMap FromFirestore(object value)
    {
        return value switch
        {
            "unspecified" => GameMap.Unspecified,
            "default" => GameMap.Default,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}
