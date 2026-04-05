using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public interface ICartRepository
{
    Task<List<CartItem>> GetCartAsync(string userId);
    Task AddToCartAsync(string userId, CartItem item);
    Task UpdateQuantityAsync(string userId, string cartItemId, int newQuantity);
    Task RemoveFromCartAsync(string userId, string cartItemId);
    Task ClearCartAsync(string userId);
}
