using Billing.Domain;
using Billing.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Billing.Api.Controllers;

[ApiController, Route("api/menu")]
public sealed class MenuController(BillingDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<MenuItem>> Get(CancellationToken cancellationToken) => await db.MenuItems.Include(item => item.Category).Where(item => item.IsAvailable).OrderBy(item => item.Category!.Name).ThenBy(item => item.Name).ToListAsync(cancellationToken);

    [Authorize(Roles = "Admin"), HttpPost]
    public async Task<ActionResult<MenuItem>> Create(MenuItem item, CancellationToken cancellationToken)
    {
        item.Id = 0; item.CreatedAt = DateTime.UtcNow; item.UpdatedAt = DateTime.UtcNow;
        db.MenuItems.Add(item); await db.SaveChangesAsync(cancellationToken); return Created($"api/menu/{item.Id}", item);
    }

    [Authorize(Roles = "Admin"), HttpPut("{id:int}")]
    public async Task<ActionResult<MenuItem>> Update(int id, MenuItem input, CancellationToken cancellationToken)
    {
        var item = await db.MenuItems.FindAsync([id], cancellationToken);
        if (item is null) return NotFound();
        item.Name = input.Name; item.Description = input.Description; item.CategoryId = input.CategoryId; item.Price = input.Price; item.GSTPercentage = input.GSTPercentage; item.IsAvailable = input.IsAvailable; item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken); return item;
    }
}
