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
            watch.CreatedAt = DateTime.UtcNow;
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
        var snapshot = await collection.Limit(5).GetSnapshotAsync();

        // Check if we need to seed: either no documents, or existing docs are corrupted (empty Brand)
        bool needsSeed = snapshot.Documents.Count == 0;
        if (!needsSeed && snapshot.Documents.Count > 0)
        {
            var sample = snapshot.Documents[0].ConvertTo<Watch>();
            if (string.IsNullOrEmpty(sample.Brand) && string.IsNullOrEmpty(sample.Name))
            {
                // Corrupted data — delete all and re-seed
                Console.WriteLine("Detected corrupted watch data. Purging and re-seeding...");
                var allDocs = await collection.GetSnapshotAsync();
                foreach (var doc in allDocs.Documents)
                {
                    await doc.Reference.DeleteAsync();
                }
                needsSeed = true;
            }
        }

        if (needsSeed)
        {
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
                        Console.WriteLine($"Seeding {rawWatches.Count} watches into Firestore...");
                        foreach (var raw in rawWatches)
                        {
                            var w = new Watch
                            {
                                Brand = raw.GetProperty("Brand").GetString() ?? "Unknown",
                                Name = raw.GetProperty("Name").GetString() ?? "Unknown",
                                Model = raw.GetProperty("Model").GetString() ?? "",
                                Description = raw.GetProperty("Description").GetString() ?? "",
                                Movement = raw.GetProperty("Movement").GetString() ?? "",
                                ImageUrl = raw.GetProperty("ImageUrl").GetString() ?? "",
                                Price = raw.GetProperty("Price").GetDouble(),
                                StockQuantity = raw.GetProperty("InStock").GetBoolean() ? 5 : 0,
                            };
                            await CreateAsync(w);
                        }
                        Console.WriteLine("Watch seeding complete.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration Seed Error: {ex.Message}");
            }
        }
    }
}
