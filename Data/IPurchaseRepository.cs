using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public interface IPurchaseRepository
{
    Task<Purchase> CreateAsync(Purchase purchase);
    Task<List<Purchase>> GetByUserIdAsync(string userId);
}