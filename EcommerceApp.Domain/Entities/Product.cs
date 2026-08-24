namespace EcommerceApp.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Category { get; set; } = string.Empty; 
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable => StockQuantity > 0;
}