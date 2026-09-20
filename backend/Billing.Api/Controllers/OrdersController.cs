using Billing.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Controllers;

[ApiController, Route("api/orders")]
public sealed class OrdersController(BillingDbContext db, OrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = db.Orders.Include(order => order.Items).AsNoTracking().OrderByDescending(order => order.OrderDate).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(order => order.OrderNumber.Contains(search));
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
