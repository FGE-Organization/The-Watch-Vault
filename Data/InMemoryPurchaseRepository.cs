using Google.Cloud.Firestore;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public sealed class InMemoryPurchaseRepository : IPurchaseRepository
{
    private readonly List<Purchase> _purchases = new();
    private readonly object _lock = new();

    public Task<Purchase> CreateAsync(Purchase purchase)
    {
        lock (_lock)
        {
            purchase.PurchasedAt = Timestamp.GetCurrentTimestamp();
            purchase.Id = Guid.NewGuid().ToString("n");
            _purchases.Add(purchase);
            return Task.FromResult(purchase);
        }
    }

    public Task<List<Purchase>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(new List<Purchase>());

        lock (_lock)
        {
            var list = _purchases
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PurchasedAt)
                .ToList();
            return Task.FromResult(list);
        }
    }
}
