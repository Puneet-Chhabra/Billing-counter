using Billing.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Billing.Api.Controllers;

[ApiController, Route("api/orders")]
[Authorize(Roles = "Admin,BillingStaff")]
public sealed class OrdersController(BillingDbContext db, OrderService orderService, Billing.Application.BillingCalculator calculator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? paymentMethod, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = db.Orders.Include(order => order.Items).AsNoTracking().OrderByDescending(order => order.OrderDate).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(order => order.OrderNumber.ToLower().Contains(normalizedSearch) || order.Items.Any(item => item.ItemName.ToLower().Contains(normalizedSearch)));
        }
        if (from.HasValue) query = query.Where(order => order.OrderDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(order => order.OrderDate < to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(paymentMethod) && !string.Equals(paymentMethod, "All", StringComparison.OrdinalIgnoreCase)) query = query.Where(order => order.PaymentMethod == paymentMethod);
        var total = await query.CountAsync(cancellationToken);
        var totalAmount = await query.SumAsync(order => (decimal?)order.GrandTotal, cancellationToken) ?? 0;
        var items = await query.Skip((Math.Max(page, 1) - 1) * pageSize).Take(Math.Clamp(pageSize, 1, 100)).ToListAsync(cancellationToken);
        return Ok(new { total, totalAmount, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) => await db.Orders.Include(order => order.Items).AsNoTracking().SingleOrDefaultAsync(order => order.Id == id, cancellationToken) is { } order ? Ok(order) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0 || string.IsNullOrWhiteSpace(request.PaymentMethod)) return BadRequest("Items and payment method are required.");
        if (!new[] { "Cash", "UPI", "Card", "Other" }.Contains(request.PaymentMethod, StringComparer.OrdinalIgnoreCase)) return BadRequest("Invalid payment method.");
        if (request.Discount < 0 || request.Items.Any(item => item.Quantity <= 0)) return BadRequest("Discount and quantities must be non-negative.");
        try { return Created("api/orders", await orderService.CreateAsync(request, User.Identity?.Name ?? "unknown", cancellationToken)); }
        catch (KeyNotFoundException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0) return Ok(new { subtotal = 0m, discount = 0m, tax = 0m, grandTotal = 0m });
        var ids = request.Items.Select(item => item.MenuItemId).Distinct().ToList();
        var menuItems = await db.MenuItems.Where(item => ids.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        if (menuItems.Count != ids.Count) return BadRequest("One or more menu items do not exist.");
        var settings = await db.BusinessSettings.SingleAsync(cancellationToken);
        try
        {
            var result = calculator.Calculate(request.Items.Select(item => (menuItems[item.MenuItemId], item.Quantity)), request.Discount, settings.TaxEnabled, settings.DefaultGSTPercentage);
            return Ok(new { result.Subtotal, result.Discount, result.Tax, result.GrandTotal });
        }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        return Conflict("Completed orders cannot be edited. Create a new order or cancel the original instead.");
    }
}
