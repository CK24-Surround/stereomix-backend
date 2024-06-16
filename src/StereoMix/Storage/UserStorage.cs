using StereoMix.Firestore;

namespace StereoMix.Storage;

public interface IUserStorage
{
    ValueTask<StorageResponse> CreateUserAccountAsync(UserStorageData newUserData, CancellationToken cancellationToken = default);
    ValueTask<UserStorageData?> GetUserAccountAsync(string userId, CancellationToken cancellationToken = default);
    ValueTask<StorageResponse> UpdateUserAccountAsync(string userId, Action<UserStorageData> updateUserData, CancellationToken cancellationToken = default);
    ValueTask<StorageResponse> DeleteUserAccountAsync(string userId, CancellationToken cancellationToken = default);
}

public class UserStorage(IFirestoreClient firestore) : Storage<UserStorageData>(firestore, "users"), IUserStorage
{
    public ValueTask<StorageResponse> CreateUserAccountAsync(UserStorageData newUserData, CancellationToken cancellationToken = default)
    {
        return CreateAsync(newUserData.Id, newUserData, cancellationToken);
    }

    public ValueTask<UserStorageData?> GetUserAccountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return GetAsync(userId, cancellationToken);
    }

    public ValueTask<StorageResponse> UpdateUserAccountAsync(string userId, Action<UserStorageData> updateUserData, CancellationToken cancellationToken = default)
    {
        return SetAsync(userId, updateUserData, cancellationToken);
    }

    public ValueTask<StorageResponse> DeleteUserAccountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(userId, cancellationToken);
    }
}
