using Google.Cloud.Firestore;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public class FirestoreCartRepository : ICartRepository
{
    private readonly FirestoreDb _db;

    public FirestoreCartRepository(FirestoreDb db)
    {
        _db = db;
    }

    private CollectionReference CartCollection(string userId)
        => _db.Collection("users").Document(userId).Collection("cart");

    public async Task<List<CartItem>> GetCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new List<CartItem>();

        var snapshot = await CartCollection(userId).GetSnapshotAsync();

        var items = new List<CartItem>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                var item = doc.ConvertTo<CartItem>();
                items.Add(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cart] Skipping corrupt doc {doc.Id}: {ex.Message}");
            }
        }
        return items;
    }

    public async Task AddToCartAsync(string userId, CartItem item)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            Console.WriteLine("[Cart] AddToCartAsync called with empty userId — aborting.");
            return;
        }

        Console.WriteLine($"[Cart] Adding {item.Name} for user {userId}");
        item.AddedAt = Timestamp.GetCurrentTimestamp();

        var coll = CartCollection(userId);

        // Check if this watch is already in the cart
        var existing = await coll.WhereEqualTo("WatchId", item.WatchId).Limit(1).GetSnapshotAsync();

        if (existing.Documents.Count > 0)
        {
            var existingDoc = existing.Documents[0];
            var existingItem = existingDoc.ConvertTo<CartItem>();
            existingItem.Quantity += item.Quantity;
            Console.WriteLine($"[Cart] Watch already in cart. New qty = {existingItem.Quantity}");
            await existingDoc.Reference.SetAsync(existingItem, SetOptions.MergeAll);
        }
        else
        {
            var docRef = await coll.AddAsync(item);
            item.Id = docRef.Id;
            Console.WriteLine($"[Cart] Added new cart document: {docRef.Id}");
        }
    }

    public async Task UpdateQuantityAsync(string userId, string cartItemId, int newQuantity)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(cartItemId))
            return;

        var docRef = CartCollection(userId).Document(cartItemId);
        var snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists) return;

        if (newQuantity <= 0)
        {
            await docRef.DeleteAsync();
        }
        else
        {
            await docRef.UpdateAsync("Quantity", newQuantity);
        }
    }

    public async Task RemoveFromCartAsync(string userId, string cartItemId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(cartItemId))
            return;

        await CartCollection(userId).Document(cartItemId).DeleteAsync();
    }

    public async Task ClearCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var snapshot = await CartCollection(userId).GetSnapshotAsync();
        foreach (var doc in snapshot.Documents)
        {
            await doc.Reference.DeleteAsync();
        }
    }
}
