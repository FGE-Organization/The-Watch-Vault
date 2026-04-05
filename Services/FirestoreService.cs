using Google.Cloud.Firestore;

namespace The_Watch_Vault.Services;

public class FirestoreService : IFirestoreService
{
    internal readonly FirestoreDb _db;

    public FirestoreService(IConfiguration configuration)
    {
        var projectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Firebase:ProjectId is not configured in appsettings.json.");

        // Program.cs may already set GOOGLE_APPLICATION_CREDENTIALS (e.g. from Firebase:CredentialsJson on Render).
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")))
        {
            var credentialsPath = configuration["Firebase:CredentialsPath"];
            if (!string.IsNullOrEmpty(credentialsPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            }
        }

        _db = FirestoreDb.Create(projectId);
    }

    public async Task<List<WatchItem>> GetWatchesAsync()
    {
        var snapshot = await _db.Collection("watches").GetSnapshotAsync();
        return snapshot.Documents
            .Select(doc => (id: doc.Id, w: doc.ConvertTo<WatchDocument>()))
            .Where(x => !string.IsNullOrEmpty(x.w.Name))
            .Select(x => new WatchItem(
                Id: x.id,
                Brand: x.w.Brand ?? "",
                CreatedAt: x.w.CreatedAt.ToDateTime(),
                Description: x.w.Description ?? "",
                ImageUrl: x.w.ImageUrl ?? "",
                InStock: x.w.InStock,
                Model: x.w.Model ?? "",
                Movement: x.w.Movement ?? "",
                Name: x.w.Name ?? "",
                Price: (decimal)x.w.Price
            )).ToList();
    }

    public async Task AddWatchAsync(string brand, string name, string model, string movement,
        string description, string imageUrl, decimal price, bool inStock)
    {
        await _db.Collection("watches").AddAsync(new Dictionary<string, object>
        {
            ["brand"]       = brand,
            ["name"]        = name,
            ["model"]       = model,
            ["movement"]    = movement,
            ["description"] = description,
            ["imageURL"]    = imageUrl,
            ["price"]       = (double)price,
            ["inStock"]     = inStock,
            ["createdAt"]   = Timestamp.GetCurrentTimestamp(),
        });
    }

    public async Task DeleteWatchAsync(string id)
    {
        await _db.Collection("watches").Document(id).DeleteAsync();
    }

    public async Task UpdateInStockAsync(string id, bool inStock)
    {
        await _db.Collection("watches").Document(id).UpdateAsync("inStock", inStock);
    }

    public async Task<List<BrandItem>> GetBrandsAsync()
    {
        var snapshot = await _db.Collection("brands").GetSnapshotAsync();
        return snapshot.Documents
            .Select(doc => doc.ConvertTo<BrandDocument>())
            .Where(b => !string.IsNullOrEmpty(b.Name))
            .Select(b => new BrandItem(
                Id: "",
                Description: b.Description ?? "",
                LogoUrl: b.LogoUrl ?? "",
                Name: b.Name!
            )).ToList();
    }
}

[FirestoreData]
public class WatchDocument
{
    [FirestoreProperty("brand")]       public string?    Brand       { get; set; }
    [FirestoreProperty("createdAt")]   public Timestamp  CreatedAt   { get; set; }
    [FirestoreProperty("description")] public string?    Description { get; set; }
    [FirestoreProperty("imageURL")]    public string?    ImageUrl    { get; set; }
    [FirestoreProperty("inStock")]     public bool       InStock     { get; set; }
    [FirestoreProperty("model")]       public string?    Model       { get; set; }
    [FirestoreProperty("movement")]    public string?    Movement    { get; set; }
    [FirestoreProperty("name")]        public string?    Name        { get; set; }
    [FirestoreProperty("price")]       public double     Price       { get; set; }
}

[FirestoreData]
public class BrandDocument
{
    [FirestoreProperty("description")] public string? Description { get; set; }
    [FirestoreProperty("logoURL")]     public string? LogoUrl     { get; set; }
    [FirestoreProperty("name")]        public string? Name        { get; set; }
}

public record WatchItem(
    string Id,
    string Brand,
    DateTime CreatedAt,
    string Description,
    string ImageUrl,
    bool InStock,
    string Model,
    string Movement,
    string Name,
    decimal Price
);

public record BrandItem(string Id, string Description, string LogoUrl, string Name);
