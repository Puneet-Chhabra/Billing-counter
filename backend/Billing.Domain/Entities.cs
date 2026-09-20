namespace Billing.Domain;

public enum OrderStatus { Completed, Cancelled }

public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<MenuItem> MenuItems { get; set; } = [];
}

public sealed class MenuItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal Price { get; set; }
    public bool IsVegetarian { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Order
{
    public int Id { get; set; }
    public required string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public required string PaymentMethod { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Completed;
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int MenuItemId { get; set; }
    public required string ItemName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool IsVegetarian { get; set; } = true;
    public decimal Total { get; set; }
}

public sealed class BusinessSettings
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = "Kitchen Counter";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public bool TaxEnabled { get; set; } = true;
    public decimal DefaultGSTPercentage { get; set; } = 5;
    public string Currency { get; set; } = "INR";
    public string BillPrefix { get; set; } = "ORD";
}
