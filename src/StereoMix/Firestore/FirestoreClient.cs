using Google.Cloud.Firestore;

namespace StereoMix.Firestore;

public interface IFirestoreClient
{
    FirestoreDb Database { get; }
}

public class FirestoreClient : IFirestoreClient
{
    private const string FirestoreProjectIdEnvName = "FIRESTORE_PROJECT_ID";

    public FirestoreClient(IConfiguration configuration)
    {
        var projectId = configuration[FirestoreProjectIdEnvName] ?? throw new InvalidOperationException($"{FirestoreProjectIdEnvName} is not set");
        Database = FirestoreDb.Create(projectId);
    }

    public FirestoreDb Database { get; }
}
