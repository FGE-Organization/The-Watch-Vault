using Google.Cloud.Firestore;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public sealed class InMemoryCartRepository : ICartRepository
{
    private readonly Dictionary<string, List<CartItem>> _carts = new(StringComparer.Ordinal);

    public Task<List<CartItem>> GetCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(new List<CartItem>());
        lock (_carts)
        {
            return _carts.TryGetValue(userId, out var list)
                ? Task.FromResult(list.ToList())
                : Task.FromResult(new List<CartItem>());
        }
    }

    public Task AddToCartAsync(string userId, CartItem item)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.CompletedTask;

        lock (_carts)
        {
            if (!_carts.TryGetValue(userId, out var list))
            {
                list = new List<CartItem>();
                _carts[userId] = list;
            }

            item.AddedAt = Timestamp.GetCurrentTimestamp();
            var existing = list.FirstOrDefault(x => x.WatchId == item.WatchId);
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
            }
            else
            {
                item.Id = Guid.NewGuid().ToString("n");
                list.Add(item);
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateQuantityAsync(string userId, string cartItemId, int newQuantity)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(cartItemId))
            return Task.CompletedTask;

        lock (_carts)
        {
            if (!_carts.TryGetValue(userId, out var list))
                return Task.CompletedTask;

            var item = list.FirstOrDefault(x => x.Id == cartItemId);
            if (item == null)
                return Task.CompletedTask;

            if (newQuantity <= 0)
                list.Remove(item);
            else
                item.Quantity = newQuantity;
        }

        return Task.CompletedTask;
    }

    public Task RemoveFromCartAsync(string userId, string cartItemId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(cartItemId))
            return Task.CompletedTask;

        lock (_carts)
        {
            if (_carts.TryGetValue(userId, out var list))
                list.RemoveAll(x => x.Id == cartItemId);
        }

        return Task.CompletedTask;
    }

    public Task ClearCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.CompletedTask;

        lock (_carts)
        {
            if (_carts.TryGetValue(userId, out var list))
                list.Clear();
        }

        return Task.CompletedTask;
    }
}
