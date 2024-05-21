using Google.Cloud.Firestore;
using StereoMix.Firestore;

namespace StereoMix.Storage;

public abstract class Storage<T>(IFirestoreClient firestore, string collectionName) where T : class
{
    protected CollectionReference Collection => firestore.Database.Collection(collectionName);

    protected async ValueTask<StorageResponse> CreateAsync(string documentId, T documentData, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentNullException.ThrowIfNull(documentData);


        return await firestore.Database.RunTransactionAsync(async transaction =>
        {
            var documentRef = Collection.Document(documentId);
            var snapshot = await transaction.GetSnapshotAsync(documentRef, cancellationToken).ConfigureAwait(false);
            if (snapshot.Exists)
            {
                return StorageResponse.AlreadyExists;
            }

            transaction.Create(documentRef, documentData);
            return StorageResponse.Success;
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    protected async ValueTask<T?> GetAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        var documentRef = Collection.Document(documentId);
        var snapshot = await documentRef.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

        return !snapshot.Exists ? null : snapshot.ConvertTo<T>();
    }

    protected async ValueTask<StorageResponse> UpdateAsync(string documentId, Action<T> updateDocumentObject, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentNullException.ThrowIfNull(updateDocumentObject);

        return await firestore.Database.RunTransactionAsync(async transaction =>
        {
            var documentRef = Collection.Document(documentId);
            var snapshot = await transaction.GetSnapshotAsync(documentRef, cancellationToken).ConfigureAwait(false);
            if (!snapshot.Exists)
            {
                return StorageResponse.NotFound;
            }

            var documentObject = snapshot.ConvertTo<T>();
            updateDocumentObject(documentObject);
            transaction.Set(documentRef, documentObject, SetOptions.MergeAll);
            return StorageResponse.Success;
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    protected async ValueTask<StorageResponse> DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        return await firestore.Database.RunTransactionAsync(async transaction =>
        {
            var documentRef = Collection.Document(documentId);
            var snapshot = await transaction.GetSnapshotAsync(documentRef, cancellationToken).ConfigureAwait(false);
            if (!snapshot.Exists)
            {
                return StorageResponse.NotFound;
            }

            transaction.Delete(documentRef);
            return StorageResponse.Success;
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    protected async ValueTask<T?> FindAsync(Query query, Func<DocumentSnapshot, bool> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var querySnapshot = await query.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return querySnapshot.Count == 0 ? null : querySnapshot.FirstOrDefault(predicate)?.ConvertTo<T>();
    }
}
