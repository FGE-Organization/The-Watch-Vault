using Google.Cloud.Firestore;
using The_Watch_Vault.Data;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Services;

/// <summary>
/// Used when Firestore is not configured: serves catalog data from <see cref="IWatchRepository"/> so UI matches seeded in-memory data.
/// </summary>
public sealed class InMemoryCatalogFirestoreService : IFirestoreService
{
    private readonly IWatchRepository _watches;

    public InMemoryCatalogFirestoreService(IWatchRepository watches)
    {
        _watches = watches;
    }

    public async Task<List<WatchItem>> GetWatchesAsync()
    {
        var list = await _watches.GetAllAsync();
        return list.Select(w => new WatchItem(
            w.Id ?? "",
            w.Brand,
            w.CreatedAt.ToDateTime(),
            w.Description,
            w.ImageUrl,
            w.InStock,
            w.Model,
            w.Movement,
            w.Name,
            (decimal)w.Price
        )).ToList();
    }

    public async Task AddWatchAsync(string brand, string name, string model, string movement,
        string description, string imageUrl, decimal price, bool inStock)
    {
        var watch = new Watch
        {
            Brand = brand,
            Name = name,
            Model = model,
            Movement = movement,
            Description = description,
            ImageUrl = imageUrl,
            Price = (double)price,
            StockQuantity = inStock ? 5 : 0,
            CreatedAt = Timestamp.GetCurrentTimestamp()
        };
        await _watches.CreateAsync(watch);
    }

    public Task DeleteWatchAsync(string id) => _watches.DeleteAsync(id);

    public async Task UpdateInStockAsync(string id, bool inStock)
    {
        var w = await _watches.GetByIdAsync(id);
        if (w == null)
            return;

        w.StockQuantity = inStock ? 5 : 0;
        await _watches.UpdateAsync(w);
    }

    public async Task<List<BrandItem>> GetBrandsAsync()
    {
        var list = await _watches.GetAllAsync();
        return list
            .Select(w => w.Brand)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b)
            .Select(b => new BrandItem("", "", "", b))
            .ToList();
    }
}
