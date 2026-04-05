using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

/// <summary>
/// In-memory implementation of IUserRepository.
/// This implementation stores users in a thread-safe dictionary.
/// It can be easily replaced with a database implementation (SQL Server, DynamoDB, etc.)
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    // Thread-safe storage for users
    private readonly Dictionary<string, User> _users = new();
    private readonly Dictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public Task<User> CreateAsync(User user)
    {
        lock (_lock)
        {
            // Check if email already exists
            if (_usersByEmail.ContainsKey(user.Email))
            {
                throw new InvalidOperationException($"A user with email '{user.Email}' already exists.");
            }

            // Assign ID and timestamps
            user.Id = Guid.NewGuid().ToString();
            user.CreatedAt = Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp();

            // Store in both dictionaries
            _users[user.Id] = user;
            _usersByEmail[user.Email] = user;

            return Task.FromResult(user);
        }
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        lock (_lock)
        {
            _usersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }
    }

    public Task<User?> GetByIdAsync(string id)
    {
        lock (_lock)
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }
    }

    public Task UpdateAsync(User user)
    {
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(user.Id) && _users.ContainsKey(user.Id))
            {
                _users[user.Id] = user;
                _usersByEmail[user.Email] = user;
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsByEmailAsync(string email)
    {
        lock (_lock)
        {
            return Task.FromResult(_usersByEmail.ContainsKey(email));
        }
    }
}