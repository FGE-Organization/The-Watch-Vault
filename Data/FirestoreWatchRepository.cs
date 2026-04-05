using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using The_Watch_Vault.Models;

namespace The_Watch_Vault.Data;

public class FirestoreWatchRepository : IWatchRepository
{
    private readonly FirestoreDb _firestoreDb;
    private const string CollectionName = "watches";
    private readonly IWebHostEnvironment _env;

    public FirestoreWatchRepository(FirestoreDb firestoreDb, IWebHostEnvironment env)
    {
        _firestoreDb = firestoreDb;
        _env = env;
    }

    public async Task<List<Watch>> GetAllAsync()
    {
        var collection = _firestoreDb.Collection(CollectionName);
        var snapshot = await collection.GetSnapshotAsync();
        
        return snapshot.Documents.Select(d => d.ConvertTo<Watch>()).ToList();
    }

    public async Task<Watch?> GetByIdAsync(string id)
    {
        var docRef = _firestoreDb.Collection(CollectionName).Document(id);
        var snapshot = await docRef.GetSnapshotAsync();
        
        if (snapshot.Exists)
        {
            return snapshot.ConvertTo<Watch>();
        }
        return null;
    }

    public async Task<Watch> CreateAsync(Watch watch)
    {
        if (watch.CreatedAt == default)
        {
            watch.CreatedAt = Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp();
        }

        var collection = _firestoreDb.Collection(CollectionName);
        var docRef = await collection.AddAsync(watch);
        
        watch.Id = docRef.Id;
        return watch;
    }

    public async Task UpdateAsync(Watch watch)
    {
        if (string.IsNullOrEmpty(watch.Id))
        {
            throw new ArgumentException("Watch ID is required for update.");
        }

        var docRef = _firestoreDb.Collection(CollectionName).Document(watch.Id);
        await docRef.SetAsync(watch, SetOptions.MergeAll);
    }

    public async Task DeleteAsync(string id)
    {
        var docRef = _firestoreDb.Collection(CollectionName).Document(id);
        await docRef.DeleteAsync();
    }

    public async Task CheckAndSeedAsync()
    {
        var collection = _firestoreDb.Collection(CollectionName);
        var snapshot = await collection.GetSnapshotAsync();

        // Selective Cleanup: Find and delete corrupted "placeholder" entries
        var corruptedDocs = snapshot.Documents.Where(doc =>
        {
            var data = doc.ToDictionary();
            bool hasInvalidBrand = !data.ContainsKey("Brand") || string.IsNullOrWhiteSpace(data["Brand"]?.ToString());
            bool hasInvalidName = !data.ContainsKey("Name") || string.IsNullOrWhiteSpace(data["Name"]?.ToString());
            
            // Note: If imageURL (uppercase URL) exists but ImageUrl doesn't, it might still have data but it's unmapped.
            // But if Brand and Name are missing, it's definitely a placeholder.
            return hasInvalidBrand && hasInvalidName;
        }).ToList();

        if (corruptedDocs.Any())
        {
            Console.WriteLine($"[Seeder] Found {corruptedDocs.Count} corrupted/placeholder watches. Deleting them...");
            var batch = _firestoreDb.StartBatch();
            foreach (var doc in corruptedDocs)
            {
                batch.Delete(doc.Reference);
            }
            await batch.CommitAsync();
            Console.WriteLine($"[Seeder] Selective cleanup complete.");
        }

        // Refresh snapshot after cleanup
        snapshot = await collection.GetSnapshotAsync();
        if (snapshot.Documents.Count == 0)
        {
            Console.WriteLine($"[Seeder] Watch collection is empty. Initializing from watches.json...");
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var path = System.IO.Path.Combine(_env.WebRootPath, "data", "watches.json");
                if (System.IO.File.Exists(path))
                {
                    var json = await System.IO.File.ReadAllTextAsync(path);
                    var rawWatches = JsonSerializer.Deserialize<List<JsonElement>>(json, options);

                    if (rawWatches != null)
                    {
                        foreach (var raw in rawWatches)
                        {
                            var w = new Watch
                            {
                                Brand         = raw.GetProperty("Brand").GetString() ?? "Unknown",
                                Name          = raw.GetProperty("Name").GetString() ?? "Unknown",
                                Model         = raw.GetProperty("Model").GetString() ?? "",
                                Description   = raw.GetProperty("Description").GetString() ?? "",
                                Movement      = raw.GetProperty("Movement").GetString() ?? "",
                                ImageUrl      = raw.GetProperty("ImageUrl").GetString() ?? "",
                                Price         = raw.GetProperty("Price").GetDouble(),
                                StockQuantity = raw.GetProperty("InStock").GetBoolean() ? 5 : 0,
                                CreatedAt     = Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp()
                            };
                            await CreateAsync(w);
                        }
                        Console.WriteLine("[Seeder] Initial seeding complete.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seeder] Migration Seed Error: {ex.Message}");
            }
        }
    }
}
