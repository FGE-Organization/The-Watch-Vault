using Google.Cloud.Firestore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace The_Watch_Vault.Services;

public class FirestoreSeeder
{
    private readonly FirestoreDb _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FirestoreSeeder> _logger;

    public FirestoreSeeder(FirestoreDb db, IWebHostEnvironment env, ILogger<FirestoreSeeder> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var watchesCol = _db.Collection("watches");
        var brandsCol = _db.Collection("brands");

        // Check if real watches already exist (skip template doc with empty name)
        var existing = await watchesCol.GetSnapshotAsync();
        bool hasRealData = existing.Documents.Any(d =>
            d.TryGetValue<string>("name", out var n) && !string.IsNullOrEmpty(n));

        if (hasRealData)
        {
            _logger.LogInformation("Firestore already has watch data. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding Firestore with watches...");

        var path = Path.Combine(_env.WebRootPath, "data", "watches.json");
        var json = await File.ReadAllTextAsync(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var watches = JsonSerializer.Deserialize<List<SeedWatch>>(json, options) ?? new();

        // Batch write watches (Firestore batches support up to 500 ops)
        var batch = _db.StartBatch();
        foreach (var w in watches)
        {
            var docRef = watchesCol.Document(w.Id);
            batch.Set(docRef, new Dictionary<string, object>
            {
                ["brand"]       = w.Brand,
                ["createdAt"]   = Timestamp.FromDateTime(w.CreatedAt.ToUniversalTime()),
                ["description"] = w.Description,
                ["imageURL"]    = w.ImageUrl,
                ["inStock"]     = w.InStock,
                ["model"]       = w.Model,
                ["movement"]    = w.Movement,
                ["name"]        = w.Name,
                ["price"]       = (double)w.Price,
            });
        }
        await batch.CommitAsync();
        _logger.LogInformation("Seeded {Count} watches.", watches.Count);

        // Seed unique brands
        var existingBrands = await brandsCol.GetSnapshotAsync();
        bool hasBrands = existingBrands.Documents.Any(d =>
            d.TryGetValue<string>("name", out var n) && !string.IsNullOrEmpty(n));

        if (!hasBrands)
        {
            var brandBatch = _db.StartBatch();
            var uniqueBrands = watches
                .Select(w => w.Brand)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(b => b);

            foreach (var brandName in uniqueBrands)
            {
                var docRef = brandsCol.Document();
                brandBatch.Set(docRef, new Dictionary<string, object>
                {
                    ["name"]        = brandName,
                    ["description"] = "",
                    ["logoURL"]     = "",
                });
            }
            await brandBatch.CommitAsync();
            _logger.LogInformation("Seeded brands.");
        }
    }

    private record SeedWatch(
        [property: JsonPropertyName("Id")]          string   Id,
        [property: JsonPropertyName("Brand")]       string   Brand,
        [property: JsonPropertyName("CreatedAt")]   DateTime CreatedAt,
        [property: JsonPropertyName("Description")] string   Description,
        [property: JsonPropertyName("ImageUrl")]    string   ImageUrl,
        [property: JsonPropertyName("InStock")]     bool     InStock,
        [property: JsonPropertyName("Model")]       string   Model,
        [property: JsonPropertyName("Movement")]    string   Movement,
        [property: JsonPropertyName("Name")]        string   Name,
        [property: JsonPropertyName("Price")]       decimal  Price
    );
}
