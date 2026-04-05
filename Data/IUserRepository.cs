using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

/// <summary>
/// Repository interface for user data access.
/// Designed to support multiple data stores (in-memory, SQL Server, DynamoDB, etc.)
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Creates a new user in the data store.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <returns>The created user with assigned ID.</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Finds a user by their email address.
    /// </summary>
    /// <param name="email">The email to search for.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Finds a user by their ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetByIdAsync(string id);

    /// <summary>
    /// Updates an existing user's data.
    /// </summary>
    Task UpdateAsync(User user);

    /// <summary>
    /// Checks if a user with the given email already exists.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <returns>True if a user exists, false otherwise.</returns>
    Task<bool> ExistsByEmailAsync(string email);
}