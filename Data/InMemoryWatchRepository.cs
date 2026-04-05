using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Hosting;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public sealed class InMemoryWatchRepository : IWatchRepository
{
    private readonly List<Watch> _watches = new();
    private readonly object _lock = new();
    private readonly IWebHostEnvironment _env;

    public InMemoryWatchRepository(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Task<List<Watch>> GetAllAsync()
    {
        lock (_lock)
            return Task.FromResult(_watches.ToList());
    }

    public Task<Watch?> GetByIdAsync(string id)
    {
        lock (_lock)
        {
            var w = _watches.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(w);
        }
    }

    public Task<Watch> CreateAsync(Watch watch)
    {
        lock (_lock)
        {
            if (watch.CreatedAt == default)
                watch.CreatedAt = Timestamp.GetCurrentTimestamp();
            watch.Id = Guid.NewGuid().ToString("n");
            _watches.Add(watch);
            return Task.FromResult(watch);
        }
    }

    public Task UpdateAsync(Watch watch)
    {
        lock (_lock)
        {
            var i = _watches.FindIndex(x => x.Id == watch.Id);
            if (i >= 0)
                _watches[i] = watch;
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(string id)
    {
        lock (_lock)
        {
            _watches.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    public async Task CheckAndSeedAsync()
    {
        lock (_lock)
        {
            if (_watches.Count > 0)
                return;
        }

        var path = Path.Combine(_env.WebRootPath, "data", "watches.json");
        if (!File.Exists(path))
            return;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = await File.ReadAllTextAsync(path);
            var rawWatches = JsonSerializer.Deserialize<List<JsonElement>>(json, options);
            if (rawWatches == null)
                return;

            lock (_lock)
            {
                foreach (var raw in rawWatches)
                {
                    var w = new Watch
                    {
                        Id = Guid.NewGuid().ToString("n"),
                        Brand = raw.GetProperty("Brand").GetString() ?? "Unknown",
                        Name = raw.GetProperty("Name").GetString() ?? "Unknown",
                        Model = raw.GetProperty("Model").GetString() ?? "",
                        Description = raw.GetProperty("Description").GetString() ?? "",
                        Movement = raw.GetProperty("Movement").GetString() ?? "",
                        ImageUrl = raw.GetProperty("ImageUrl").GetString() ?? "",
                        Price = raw.GetProperty("Price").GetDouble(),
                        StockQuantity = raw.GetProperty("InStock").GetBoolean() ? 5 : 0,
                        CreatedAt = Timestamp.GetCurrentTimestamp()
                    };
                    _watches.Add(w);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InMemoryWatchRepository] Seed error: {ex.Message}");
        }
    }
}
