using StereoMix.Firestore;
using StereoMix.Lobby;

namespace StereoMix.Storage;

public interface ILobbyStorage
{
    ValueTask<StorageResponse> CreateRoomAsync(LobbyStorageData newRoomData, CancellationToken cancellationToken = default);
    ValueTask<LobbyStorageData?> GetRoomAsync(string roomId, CancellationToken cancellationToken = default);
    ValueTask<LobbyStorageData?> FindRoomByShortIdAsync(string gameVersion, string shortRoomId, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyCollection<LobbyStorageData>> GetRoomsAsync(string gameVersion, GameMode mode, GameMap map, CancellationToken cancellationToken = default);
    ValueTask<StorageResponse> SetRoomAsync(string roomId, Action<LobbyStorageData> setRoomData, CancellationToken cancellationToken = default);
}

public class LobbyStorage(IFirestoreClient firestore) : Storage<LobbyStorageData>(firestore, "rooms"), ILobbyStorage
{
    public ValueTask<StorageResponse> CreateRoomAsync(LobbyStorageData newRoomData, CancellationToken cancellationToken = default)
    {
        return CreateAsync(newRoomData.RoomId, newRoomData, cancellationToken);
    }

    public ValueTask<LobbyStorageData?> GetRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        return GetAsync(roomId, cancellationToken);
    }

    public ValueTask<LobbyStorageData?> FindRoomByShortIdAsync(string gameVersion, string shortRoomId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shortRoomId, nameof(shortRoomId));
        var query = Collection.WhereEqualTo("short_id", shortRoomId);
        return FindAsync(query, snapshot => snapshot.ConvertTo<LobbyStorageData>().State == RoomState.Open, cancellationToken);
    }

    public ValueTask<IReadOnlyCollection<LobbyStorageData>> GetRoomsAsync(string gameVersion, GameMode mode, GameMap map, CancellationToken cancellationToken = default)
    {
        var query = Collection
            .WhereEqualTo("game_version", gameVersion)
            .WhereEqualTo("state", RoomState.Open)
            .WhereEqualTo("visibility", RoomVisibility.Public);

        if (mode != GameMode.Unspecified)
        {
            query = query.WhereEqualTo("mode", mode);
        }

        if (map != GameMap.Unspecified)
        {
            query = query.WhereEqualTo("map", map);
        }

        return FindAllAsync(query, cancellationToken);
    }

    public ValueTask<StorageResponse> SetRoomAsync(string roomId, Action<LobbyStorageData> setRoomData, CancellationToken cancellationToken = default)
    {
        return SetAsync(roomId, setRoomData, cancellationToken);
    }
}
