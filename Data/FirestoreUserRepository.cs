using Google.Cloud.Firestore;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public class FirestoreUserRepository : IUserRepository
{
    private readonly FirestoreDb _firestoreDb;
    private const string CollectionName = "users";

    public FirestoreUserRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<User> CreateAsync(User user)
    {
        if (await ExistsByEmailAsync(user.Email))
        {
            throw new InvalidOperationException($"A user with email '{user.Email}' already exists.");
        }

        user.CreatedAt = Timestamp.GetCurrentTimestamp();

        var collection = _firestoreDb.Collection(CollectionName);
        var docRef = await collection.AddAsync(user);
        
        user.Id = docRef.Id;
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var collection = _firestoreDb.Collection(CollectionName);
        var query = collection.WhereEqualTo("Email", email).Limit(1);
        var snapshot = await query.GetSnapshotAsync();

        if (snapshot.Documents.Count == 0) return null;

        var doc = snapshot.Documents[0];
        var user = doc.ConvertTo<User>();
        user.Id = doc.Id;
        return user;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        var docRef = _firestoreDb.Collection(CollectionName).Document(id);
        var snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists) return null;

        var user = snapshot.ConvertTo<User>();
        user.Id = snapshot.Id;
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        if (string.IsNullOrEmpty(user.Id))
            throw new ArgumentException("User ID is required for update.");

        var docRef = _firestoreDb.Collection(CollectionName).Document(user.Id);
        await docRef.SetAsync(user, SetOptions.MergeAll);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var user = await GetByEmailAsync(email);
        return user != null;
    }
}
