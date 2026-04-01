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
    private readonly Dictionary<int, User> _users = new();
    private readonly Dictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId = 1;
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
            user.Id = _nextId++;
            user.CreatedAt = DateTime.UtcNow;

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

    public Task<User?> GetByIdAsync(int id)
    {
        lock (_lock)
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
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