using Google.Cloud.Firestore;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public sealed class FirestorePurchaseRepository : IPurchaseRepository
{
    private readonly FirestoreDb _db;
    private const string CollectionName = "purchases";

    public FirestorePurchaseRepository(FirestoreDb db)
    {
        _db = db;
    }

    public async Task<Purchase> CreateAsync(Purchase purchase)
    {
        purchase.PurchasedAt = Timestamp.GetCurrentTimestamp();

        var docRef = await _db.Collection(CollectionName).AddAsync(purchase);
        purchase.Id = docRef.Id;
        Console.WriteLine($"[Purchase] Created purchase {docRef.Id}");
        return purchase;
    }

    public async Task<List<Purchase>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new List<Purchase>();

        var snapshot = await _db.Collection(CollectionName)
            .WhereEqualTo(nameof(Purchase.UserId), userId)
            .GetSnapshotAsync();

        var purchases = new List<Purchase>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                purchases.Add(doc.ConvertTo<Purchase>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Purchase] Skipping corrupt doc {doc.Id}: {ex.Message}");
            }
        }

        return purchases.OrderByDescending(p => p.PurchasedAt).ToList();
    }
}