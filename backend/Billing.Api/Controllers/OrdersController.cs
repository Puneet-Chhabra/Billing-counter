using Billing.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Billing.Api.Controllers;

[ApiController, Route("api/orders")]
[Authorize]
public sealed class OrdersController(BillingDbContext db, OrderService orderService) : ControllerBase
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
        var items = await query.Skip((Math.Max(page, 1) - 1) * pageSize).Take(Math.Clamp(pageSize, 1, 100)).ToListAsync(cancellationToken);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) => await db.Orders.Include(order => order.Items).AsNoTracking().SingleOrDefaultAsync(order => order.Id == id, cancellationToken) is { } order ? Ok(order) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0 || string.IsNullOrWhiteSpace(request.PaymentMethod)) return BadRequest("Items and payment method are required.");
        try { return Created("api/orders", await orderService.CreateAsync(request, "staff", cancellationToken)); }
        catch (KeyNotFoundException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}
