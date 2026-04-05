using Google.Cloud.Firestore;

namespace The_Watch_Vault.Models;

[FirestoreData]
public class CartItem
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string WatchId { get; set; } = "";

    [FirestoreProperty]
    public string Brand { get; set; } = "";

    [FirestoreProperty]
    public string Name { get; set; } = "";

    [FirestoreProperty]
    public string ImageUrl { get; set; } = "";

    [FirestoreProperty]
    public double UnitPrice { get; set; }

    [FirestoreProperty]
    public int Quantity { get; set; }

    [FirestoreProperty]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public double TotalPrice => UnitPrice * Quantity;
}
