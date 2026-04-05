using The_Watch_Vault.Data;

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

    public Task AddWatchAsync(string brand, string name, string model, string movement,
        string description, string imageUrl, decimal price, bool inStock) =>
        Task.CompletedTask;

    public Task DeleteWatchAsync(string id) => Task.CompletedTask;

    public Task UpdateInStockAsync(string id, bool inStock) => Task.CompletedTask;

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
