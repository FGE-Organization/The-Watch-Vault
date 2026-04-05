using Google.Cloud.Firestore;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public class FirestoreCartRepository : ICartRepository
{
    private readonly FirestoreDb _firestoreDb;

    public FirestoreCartRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    private CollectionReference GetCartCollection(string userId)
    {
        return _firestoreDb.Collection("users").Document(userId).Collection("cart");
    }

    public async Task<List<CartItem>> GetCartAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return new List<CartItem>();
        
        var snapshot = await GetCartCollection(userId).GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<CartItem>()).ToList();
    }

    public async Task AddToCartAsync(string userId, CartItem item)
    {
        if (string.IsNullOrEmpty(userId)) return;

        if (item.AddedAt == default) item.AddedAt = DateTime.UtcNow;

        var collection = GetCartCollection(userId);
        
        // Search if item with same WatchId already exists to increment quantity
        var existingSnapshot = await collection.WhereEqualTo("WatchId", item.WatchId).Limit(1).GetSnapshotAsync();
        
        if (existingSnapshot.Documents.Count > 0)
        {
            var existingDoc = existingSnapshot.Documents[0];
            var existingItem = existingDoc.ConvertTo<CartItem>();
            existingItem.Quantity += item.Quantity;
            await existingDoc.Reference.SetAsync(existingItem, SetOptions.MergeAll);
        }
        else
        {
            var docRef = await collection.AddAsync(item);
            item.Id = docRef.Id;
        }
    }

    public async Task UpdateQuantityAsync(string userId, string cartItemId, int newQuantity)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(cartItemId)) return;

        var docRef = GetCartCollection(userId).Document(cartItemId);
        var snapshot = await docRef.GetSnapshotAsync();
        
        if (snapshot.Exists)
        {
            if (newQuantity <= 0)
            {
                await docRef.DeleteAsync();
            }
            else
            {
                await docRef.UpdateAsync("Quantity", newQuantity);
            }
        }
    }

    public async Task RemoveFromCartAsync(string userId, string cartItemId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(cartItemId)) return;
        
        await GetCartCollection(userId).Document(cartItemId).DeleteAsync();
    }

    public async Task ClearCartAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        var snapshot = await GetCartCollection(userId).GetSnapshotAsync();
        foreach (var doc in snapshot.Documents)
        {
            await doc.Reference.DeleteAsync();
        }
    }
}
