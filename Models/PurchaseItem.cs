using Google.Cloud.Firestore;

namespace The_Watch_Vault.Models;

[FirestoreData]
public class PurchaseItem
{
    [FirestoreProperty]
    public string WatchId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Brand { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ImageUrl { get; set; } = string.Empty;

    [FirestoreProperty]
    public double UnitPrice { get; set; }

    [FirestoreProperty]
    public int Quantity { get; set; }

    [FirestoreProperty]
    public double LineTotal { get; set; }
}