using Google.Cloud.Firestore;

namespace The_Watch_Vault.Models;

[FirestoreData]
public class Purchase
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string UserEmail { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public List<PurchaseItem> Items { get; set; } = new();

    [FirestoreProperty]
    public double TotalAmount { get; set; }

    [FirestoreProperty]
    public string Status { get; set; } = "Completed";
}