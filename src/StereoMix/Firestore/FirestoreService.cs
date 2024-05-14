using Google.Cloud.Firestore;

namespace StereoMix.Firestore;

public interface IFirestoreService
{
    ValueTask<FirestoreDb> GetDatabaseAsync();
}

public class FirestoreService(ILogger<FirestoreService> logger) : IFirestoreService
{
    private FirestoreDb? _database;

    public async ValueTask<FirestoreDb> GetDatabaseAsync()
    {
        if (_database is not null)
        {
            return _database;
        }

        var projectId = Environment.GetEnvironmentVariable("FIRESTORE_PROJECT_ID") ?? throw new InvalidOperationException("FIRESTORE_PROJECT_ID is not set");
        // var credential = await GoogleCredential.GetApplicationDefaultAsync().ConfigureAwait(false);

        _database = await FirestoreDb.CreateAsync(projectId).ConfigureAwait(false);
        return _database;
    }
}
