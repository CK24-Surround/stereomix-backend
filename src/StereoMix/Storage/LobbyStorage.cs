using StereoMix.Firestore;
using StereoMix.Lobby;

namespace StereoMix.Storage;

public interface ILobbyStorage
{
    ValueTask<StorageResponse> CreateRoomAsync(LobbyStorageData newRoomData, CancellationToken cancellationToken = default);
    ValueTask<LobbyStorageData?> GetRoomAsync(string roomId, CancellationToken cancellationToken = default);
    ValueTask<LobbyStorageData?> FindRoomByShortIdAsync(string shortRoomId, CancellationToken cancellationToken = default);
    ValueTask<StorageResponse> UpdateRoomAsync(string roomId, Action<LobbyStorageData> updateRoomData, CancellationToken cancellationToken = default);
    ValueTask<StorageResponse> CloseRoomAsync(string roomId, CancellationToken cancellationToken = default);
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

    public ValueTask<LobbyStorageData?> FindRoomByShortIdAsync(string shortRoomId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shortRoomId, nameof(shortRoomId));
        var query = Collection.WhereEqualTo("short_id", shortRoomId);
        return FindAsync(query, snapshot => snapshot.ConvertTo<LobbyStorageData>().State == RoomState.Open, cancellationToken);
    }

    public ValueTask<StorageResponse> UpdateRoomAsync(string roomId, Action<LobbyStorageData> updateRoomData, CancellationToken cancellationToken = default)
    {
        return UpdateAsync(roomId, updateRoomData, cancellationToken);
    }

    public ValueTask<StorageResponse> CloseRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(roomId, cancellationToken);
    }
}
