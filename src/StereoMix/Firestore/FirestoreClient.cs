using Google.Cloud.Firestore;

namespace StereoMix.Firestore;

public interface IFirestoreClient
{
    FirestoreDb Database { get; }
}

public class FirestoreClient : IFirestoreClient
{
    private const string FirestoreProjectIdEnvName = "FIRESTORE_PROJECT_ID";
    private const string FirestoreDatabaseIdEnvName = "FIRESTORE_DATABASE_ID";

    public FirestoreClient(IConfiguration configuration)
    {
        var projectId = configuration[FirestoreProjectIdEnvName] ?? throw new InvalidOperationException($"{FirestoreProjectIdEnvName} is not set");
        var databaseId = configuration[FirestoreDatabaseIdEnvName] ?? throw new InvalidOperationException($"{FirestoreDatabaseIdEnvName} is not set");
        var builder = new FirestoreDbBuilder { ProjectId = projectId, DatabaseId = databaseId };
        Database = builder.Build();
    }

    public FirestoreDb Database { get; }
}
