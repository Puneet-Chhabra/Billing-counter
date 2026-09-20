using Billing.Application;
using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure;

public sealed record CreateOrderLine(int MenuItemId, int Quantity);
public sealed record CreateOrderRequest(IReadOnlyList<CreateOrderLine> Items, decimal Discount, string PaymentMethod);

public sealed class OrderService(BillingDbContext db, BillingCalculator calculator)
{
    public async Task<Order> CreateAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken)
    {
        var ids = request.Items.Select(item => item.MenuItemId).Distinct().ToList();
        var menuItems = await db.MenuItems.Where(item => ids.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        if (menuItems.Count != ids.Count) throw new KeyNotFoundException("One or more menu items do not exist.");
        if (request.Items.Any(item => !menuItems[item.MenuItemId].IsAvailable)) throw new InvalidOperationException("One or more menu items are unavailable.");

        var settings = await db.BusinessSettings.SingleAsync(cancellationToken);
        var result = calculator.Calculate(request.Items.Select(item => (menuItems[item.MenuItemId], item.Quantity)), request.Discount, settings.TaxEnabled);
        var date = DateTime.UtcNow;
        var sequence = await db.Orders.CountAsync(order => order.OrderDate.Date == date.Date, cancellationToken) + 1;
        var order = new Order
        {
            OrderNumber = $"{settings.BillPrefix}-{date:yyyyMMdd}-{sequence:000}",
            Subtotal = result.Subtotal,
            Discount = result.Discount,
            Tax = result.Tax,
            GrandTotal = result.GrandTotal,
            PaymentMethod = request.PaymentMethod,
            CreatedBy = createdBy,
            Items = result.Items.Select(line => new OrderItem { MenuItemId = line.MenuItemId, ItemName = line.ItemName, UnitPrice = line.UnitPrice, Quantity = line.Quantity, GSTPercentage = line.GSTPercentage, Total = line.Total }).ToList()
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return order;
    }
}
