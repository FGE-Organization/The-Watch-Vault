using Google.Cloud.Firestore;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public sealed class FirestorePurchaseRepository : IPurchaseRepository
{
    private readonly FirestoreDb _firestoreDb;
    private const string CollectionName = "purchases";

    public FirestorePurchaseRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<Purchase> CreateAsync(Purchase purchase)
    {
        if (purchase.PurchasedAt == default)
        {
            purchase.PurchasedAt = DateTime.UtcNow;
        }

        var collection = _firestoreDb.Collection(CollectionName);
        var docRef = await collection.AddAsync(purchase);
        purchase.Id = docRef.Id;
        return purchase;
    }

    public async Task<List<Purchase>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new List<Purchase>();
        }

        var snapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(Purchase.UserId), userId)
            .OrderByDescending(nameof(Purchase.PurchasedAt))
            .GetSnapshotAsync();

        return snapshot.Documents.Select(doc => doc.ConvertTo<Purchase>()).ToList();
    }
}