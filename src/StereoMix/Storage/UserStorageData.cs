using Google.Cloud.Firestore;

namespace StereoMix.Storage;

[FirestoreData]
public class UserStorageData
{
    [FirestoreProperty("id")]  public required string Id { get; init; }
    [FirestoreProperty("name")] public required string Name { get; init; }

    public override string ToString()
    {
        return $"User: Id={Id}, Name={Name}";
    }
}
