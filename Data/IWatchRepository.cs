using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public interface IWatchRepository
{
    Task<List<Watch>> GetAllAsync();
    Task<Watch?> GetByIdAsync(string id);
    Task<Watch> CreateAsync(Watch watch);
    Task UpdateAsync(Watch watch);
    Task DeleteAsync(string id);
    Task CheckAndSeedAsync(); // Used to inject watches.json on first boot!
}
