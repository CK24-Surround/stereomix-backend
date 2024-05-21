using Google.Cloud.Firestore;

namespace StereoMix.Storage;

[FirestoreData]
public class UserStorageData
{
    [FirestoreProperty] public required string Id { get; init; }
    [FirestoreProperty] public required string Name { get; init; }

    public override string ToString()
    {
        return $"User: Id={Id}, Name={Name}";
    }
}
