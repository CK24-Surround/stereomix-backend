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

public class LobbyStorage(IFirestoreClient firestore, ILogger<LobbyStorage> logger) : Storage<LobbyStorageData>(firestore, "rooms"), ILobbyStorage
{
    public ValueTask<StorageResponse> CreateRoomAsync(LobbyStorageData newRoomData, CancellationToken cancellationToken = default)
    {
        return CreateAsync(newRoomData.RoomId, newRoomData, cancellationToken);
    }

    public ValueTask<LobbyStorageData?> GetRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        return GetAsync(roomId, cancellationToken);
    }

    public async ValueTask<LobbyStorageData?> FindRoomByShortIdAsync(string gameVersion, string shortRoomId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shortRoomId, nameof(shortRoomId));

        var query = Collection
            .WhereEqualTo("game_version", gameVersion)
            .WhereEqualTo("short_id", shortRoomId)
            .WhereEqualTo("state", "open");

        var querySnapshot = await query.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (querySnapshot.Count == 0)
        {
            return null;
        }

        foreach (var document in querySnapshot.Documents)
        {
            logger.LogInformation("{id}, {updatedTime}", document.Id, document.UpdateTime);
            var data = document.ConvertTo<LobbyStorageData>();
            logger.LogInformation("{data}", data);
        }

        return querySnapshot.Documents[0].ConvertTo<LobbyStorageData>();
    }

    public async ValueTask<IReadOnlyCollection<LobbyStorageData>> GetRoomsAsync(string gameVersion, GameMode mode, GameMap map, CancellationToken cancellationToken = default)
    {
        var query = Collection
            .WhereEqualTo("game_version", gameVersion)
            .WhereEqualTo("state", "open")
            .WhereEqualTo("visibility", "public");

        if (mode != GameMode.Unspecified)
        {
            query = query.WhereEqualTo("mode", mode);
        }

        if (map != GameMap.Unspecified)
        {
            query = query.WhereEqualTo("map", map);
        }

        var querySnapshot = await query.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (querySnapshot is null || querySnapshot.Count == 0)
        {
            return Array.Empty<LobbyStorageData>();
        }

        return querySnapshot.Documents
            .Where(document => document.UpdateTime?.ToDateTime().AddHours(1) >= DateTime.UtcNow)
            .Select(document => document.ConvertTo<LobbyStorageData>()).ToArray();
    }

    public ValueTask<StorageResponse> SetRoomAsync(string roomId, Action<LobbyStorageData> setRoomData, CancellationToken cancellationToken = default)
    {
        return SetAsync(roomId, setRoomData, cancellationToken);
    }
}
