namespace The_Watch_Vault.Services;

public interface IFirestoreService
{
    Task<List<WatchItem>> GetWatchesAsync();
    Task AddWatchAsync(string brand, string name, string model, string movement,
        string description, string imageUrl, decimal price, bool inStock);
    Task DeleteWatchAsync(string id);
    Task UpdateInStockAsync(string id, bool inStock);
    Task<List<BrandItem>> GetBrandsAsync();
}
