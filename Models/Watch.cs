using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace The_Watch_Vault.Models;

[FirestoreData]
public class Watch
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    [Required]
    public string Brand { get; set; } = "";

    [FirestoreProperty]
    [Required]
    public string Name { get; set; } = "";

    [FirestoreProperty]
    public string Model { get; set; } = "";

    [FirestoreProperty]
    public string Description { get; set; } = "";

    [FirestoreProperty]
    public string Movement { get; set; } = "";

    [FirestoreProperty]
    public string ImageUrl { get; set; } = "";

    [FirestoreProperty]
    [Required]
    public double Price { get; set; }

    [FirestoreProperty]
    public int StockQuantity { get; set; }

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();

    // Helper property for backwards compatibility
    public bool InStock => StockQuantity > 0;
}
