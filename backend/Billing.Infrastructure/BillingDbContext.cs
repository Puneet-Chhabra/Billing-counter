using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasIndex(order => order.OrderNumber).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(order => order.OrderDate);
        modelBuilder.Entity<OrderItem>().HasIndex(item => item.OrderId);
        modelBuilder.Entity<MenuItem>().HasIndex(item => new { item.CategoryId, item.IsAvailable });
        modelBuilder.Entity<MenuItem>().Property(item => item.Price).HasPrecision(18, 2);
        modelBuilder.Entity<User>().HasIndex(user => user.Username).IsUnique();
        modelBuilder.Entity<User>().Property(user => user.Username).HasMaxLength(100);
        modelBuilder.Entity<Order>().Property(order => order.Subtotal).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(order => order.Discount).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(order => order.Tax).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(order => order.GrandTotal).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(item => item.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(item => item.Total).HasPrecision(18, 2);
        modelBuilder.Entity<Category>().HasData(new Category { Id = 1, Name = "Burgers" });
        modelBuilder.Entity<MenuItem>().HasData(
            new MenuItem { Id = 1, CategoryId = 1, Name = "Classic Burger", Price = 120, GSTPercentage = 5, Description = "House beef burger" },
            new MenuItem { Id = 2, CategoryId = 1, Name = "French Fries", Price = 80, GSTPercentage = 5, Description = "Crispy salted fries" });
        modelBuilder.Entity<BusinessSettings>().HasData(new BusinessSettings { Id = 1 });
    }
}
